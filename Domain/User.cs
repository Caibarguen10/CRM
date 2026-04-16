namespace CrmService.Domain;

/// <summary>
/// Entidad que representa un usuario del sistema con capacidades de autenticación.
/// Hereda de BaseEntity para obtener auditoría y soft delete automáticos.
/// </summary>
/// <remarks>
/// Los usuarios tienen credenciales de acceso y un rol asignado que determina
/// sus permisos en el sistema. La contraseña se almacena hasheada usando BCrypt.
/// </remarks>
public class User : BaseEntity
{
	/// <summary>
	/// Nombre de usuario único para autenticación.
	/// Debe ser único en el sistema.
	/// Máximo 50 caracteres.
	/// </summary>
	/// <example>
	/// "jperez", "admin", "maria.gonzalez"
	/// </example>
	public string Username { get; set; } = null!;
	
	/// <summary>
	/// Hash BCrypt de la contraseña del usuario.
	/// NUNCA se almacena la contraseña en texto plano.
	/// El hash es generado usando BCrypt.Net.BCrypt.HashPassword().
	/// </summary>
	/// <remarks>
	/// BCrypt genera un hash que incluye el salt automáticamente.
	/// Ejemplo de hash: "$2a$11$K2D8qXKfH5xJ7Y..."
	/// </remarks>
	public string PasswordHash { get; set; } = null!;
	
	/// <summary>
	/// Dirección de correo electrónico del usuario.
	/// Debe ser única en el sistema.
	/// Se usa para recuperación de contraseña y notificaciones.
	/// Máximo 150 caracteres.
	/// </summary>
	public string Email { get; set; } = null!;
	
	/// <summary>
	/// Rol asignado al usuario que determina sus permisos.
	/// Ver <see cref="UserRole"/> para la descripción de cada rol.
	/// </summary>
	/// <remarks>
	/// Por defecto, los nuevos usuarios son creados con rol Asesor.
	/// Solo los administradores pueden asignar el rol Admin.
	/// </remarks>
	public UserRole Role { get; set; } = UserRole.Asesor;
}
