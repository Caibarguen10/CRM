using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CrmService.Data;
using CrmService.Domain;
using CrmService.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CrmService.Services;

/// <summary>
/// Servicio de autenticación y autorización del sistema CRM.
/// Gestiona el registro de usuarios, login y generación de tokens JWT.
/// </summary>
/// <remarks>
/// Este servicio proporciona las funcionalidades core de seguridad:
/// 
/// 1. REGISTRO (RegisterAsync):
///    - Valida que el username y email sean únicos
///    - Hashea la contraseña con BCrypt
///    - Crea el usuario con un rol asignado
///    - Retorna un token JWT válido
/// 
/// 2. LOGIN (LoginAsync):
///    - Valida credenciales contra la base de datos
///    - Verifica el hash de la contraseña con BCrypt
///    - Genera un token JWT con claims del usuario
/// 
/// 3. GENERACIÓN DE TOKENS JWT:
///    - Incluye claims: UserId, Username, Email, Role
///    - Tokens firmados con clave secreta
///    - Tiempo de expiración configurable
/// </remarks>
public class AuthService : IAuthService
{
	private readonly AppDbContext _context;
	private readonly IConfiguration _configuration;
	private readonly ILogger<AuthService> _logger;

	/// <summary>
	/// Constructor del servicio de autenticación.
	/// </summary>
	/// <param name="context">Contexto de base de datos para acceso a usuarios</param>
	/// <param name="configuration">Configuración de la aplicación (contiene settings de JWT)</param>
	/// <param name="logger">Logger para registro de eventos de seguridad</param>
	public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
	{
		_context = context;
		_configuration = configuration;
		_logger = logger;
	}

	/// <summary>
	/// Registra un nuevo usuario en el sistema.
	/// </summary>
	/// <param name="dto">Datos del usuario a registrar (username, password, email, role)</param>
	/// <returns>Token JWT válido con información del usuario creado</returns>
	/// <exception cref="InvalidOperationException">
	/// Se lanza si el username o email ya existen en el sistema
	/// </exception>
	/// <remarks>
	/// Proceso de registro:
	/// 1. Valida que el username no exista
	/// 2. Valida que el email no exista
	/// 3. Hashea la contraseña con BCrypt (nunca se guarda en texto plano)
	/// 4. Crea el usuario en la base de datos
	/// 5. Genera y retorna un token JWT
	/// 
	/// SEGURIDAD:
	/// - La contraseña se hashea con BCrypt antes de guardarla
	/// - BCrypt genera un salt aleatorio automáticamente
	/// - El hash incluye el algoritmo, costo y salt
	/// </remarks>
	public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
	{
		_logger.LogInformation("Registrando nuevo usuario: {Username}", dto.Username);

		// Validar que el username no exista
		if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
		{
			_logger.LogWarning("Intento de registro con usuario existente: {Username}", dto.Username);
			throw new InvalidOperationException("El nombre de usuario ya existe.");
		}

		// Validar que el email no exista
		if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
		{
			_logger.LogWarning("Intento de registro con email existente: {Email}", dto.Email);
			throw new InvalidOperationException("El email ya está registrado.");
		}

		// Crear el usuario con contraseña hasheada
		var user = new User
		{
			Username = dto.Username,
			Email = dto.Email,
			Role = dto.Role,
			// BCrypt.HashPassword genera un hash que incluye:
			// - Algoritmo (bcrypt)
			// - Costo (número de rondas, por defecto 11)
			// - Salt aleatorio de 16 bytes
			// - Hash de 24 bytes
			// Resultado: $2a$11$[22 chars de salt][31 chars de hash]
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
		};

		// Guardar en la base de datos
		// Los campos de auditoría (CreatedBy, CreatedAt) se llenan automáticamente
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_logger.LogInformation("Usuario registrado exitosamente. ID: {UserId}, Username: {Username}", user.Id, user.Username);

		// Generar y retornar token JWT
		return GenerateToken(user);
	}

	/// <summary>
	/// Autentica un usuario y genera un token JWT si las credenciales son válidas.
	/// </summary>
	/// <param name="dto">Credenciales de login (username y password)</param>
	/// <returns>Token JWT válido si las credenciales son correctas</returns>
	/// <exception cref="UnauthorizedAccessException">
	/// Se lanza si el usuario no existe o la contraseña es incorrecta
	/// </exception>
	/// <remarks>
	/// Proceso de login:
	/// 1. Busca el usuario por username
	/// 2. Verifica el hash de la contraseña con BCrypt.Verify()
	/// 3. Si las credenciales son válidas, genera un token JWT
	/// 
	/// SEGURIDAD:
	/// - No se revela si el username existe o si la contraseña es incorrecta
	/// - Siempre retorna el mismo mensaje genérico "Credenciales inválidas"
	/// - Esto previene ataques de enumeración de usuarios
	/// </remarks>
	public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
	{
		_logger.LogInformation("Intento de login: {Username}", dto.Username);

		// Buscar usuario por username
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
		
		// Verificar que el usuario existe Y que la contraseña es correcta
		// BCrypt.Verify() compara la contraseña en texto plano con el hash
		// Es seguro porque BCrypt usa timing-safe comparison
		if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
		{
			_logger.LogWarning("Login fallido para usuario: {Username}", dto.Username);
			throw new UnauthorizedAccessException("Credenciales inválidas.");
		}

		_logger.LogInformation("Login exitoso. Usuario: {Username}, Rol: {Role}", user.Username, user.Role);

		// Generar y retornar token JWT
		return GenerateToken(user);
	}

	/// <summary>
	/// Genera un token JWT (JSON Web Token) para el usuario autenticado.
	/// </summary>
	/// <param name="user">Usuario para el cual generar el token</param>
	/// <returns>Respuesta con token JWT y datos del usuario</returns>
	/// <remarks>
	/// Estructura del token JWT:
	/// 
	/// HEADER (Base64):
	/// {
	///   "alg": "HS256",  // Algoritmo de firma
	///   "typ": "JWT"     // Tipo de token
	/// }
	/// 
	/// PAYLOAD (Base64 - CLAIMS):
	/// {
	///   "nameid": "123",              // User ID
	///   "unique_name": "jperez",      // Username
	///   "email": "jperez@example.com",// Email
	///   "role": "Admin",              // Rol del usuario
	///   "exp": 1234567890,            // Timestamp de expiración
	///   "iss": "CrmServiceAPI",       // Emisor
	///   "aud": "CrmServiceClient"     // Audiencia
	/// }
	/// 
	/// SIGNATURE (HMAC-SHA256):
	/// HMACSHA256(
	///   base64UrlEncode(header) + "." + base64UrlEncode(payload),
	///   secretKey
	/// )
	/// 
	/// TOKEN FINAL: [Header].[Payload].[Signature]
	/// 
	/// SEGURIDAD:
	/// - El token está firmado pero NO encriptado
	/// - Cualquiera puede leer el payload (es base64, no encriptación)
	/// - La firma garantiza que no ha sido modificado
	/// - NUNCA incluir información sensible en el token
	/// </remarks>
	private AuthResponseDto GenerateToken(User user)
	{
		// Obtener configuración de JWT desde appsettings.json
		var jwtSettings = _configuration.GetSection("Jwt");
		var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");
		
		// Crear la clave de firma usando HMAC-SHA256
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
		var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		// Configurar tiempo de expiración
		var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");
		var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

		// CLAIMS: Información que se incluye en el token
		// Estos claims estarán disponibles en HttpContext.User después de la autenticación
		var claims = new[]
		{
			// Claim estándar para User ID
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			
			// Claim estándar para Username
			// Este es el que se obtiene con User.Identity.Name
			new Claim(ClaimTypes.Name, user.Username),
			
			// Claim estándar para Email
			new Claim(ClaimTypes.Email, user.Email),
			
			// Claim estándar para Rol
			// Usado por [Authorize(Roles = "Admin")]
			new Claim(ClaimTypes.Role, user.Role.ToString())
		};

		// Crear el token JWT
		var token = new JwtSecurityToken(
			issuer: jwtSettings["Issuer"],        // Quién emite el token
			audience: jwtSettings["Audience"],    // Para quién es el token
			claims: claims,                       // Información del usuario
			expires: expiresAt,                   // Cuándo expira
			signingCredentials: credentials       // Cómo se firma
		);

		// Convertir el token a string y retornar
		return new AuthResponseDto
		{
			Token = new JwtSecurityTokenHandler().WriteToken(token),
			Username = user.Username,
			Email = user.Email,
			Role = user.Role.ToString(),
			ExpiresAt = expiresAt
		};
	}
}
