/*
 * ====================================================================
 * CRM SERVICE - MICROSERVICIO DE GESTIÓN DE RELACIONES CON CLIENTES
 * ====================================================================
 * 
 * Este archivo configura y arranca el microservicio CRM desarrollado con .NET 8 (LTS).
 * 
 * CARACTERÍSTICAS IMPLEMENTADAS:
 * ✓ Entity Framework Core con SQLite (persistencia real)
 * ✓ Auditoría automática (CreatedBy, UpdatedBy, etc.)
 * ✓ Soft Delete (borrado lógico)
 * ✓ Autenticación JWT (JSON Web Tokens)
 * ✓ AutoMapper para mapeo de DTOs
 * ✓ Swagger UI con soporte para JWT
 * ✓ Manejo centralizado de errores
 * ✓ Logging estructurado
 * ✓ Sistema de roles (Admin, Asesor, Auditor)
 * 
 * ARQUITECTURA:
 * Controller → Service → Repository → DbContext → SQLite Database
 */

using CrmService.Data;
using CrmService.Repositories;
using CrmService.Services;
using CrmService.Middleware;
using CrmService.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// CONFIGURACIÓN DE PUERTOS PARA DEPLOY EN CLOUD
// ====================================================================

// Railway/Render asignan el puerto dinámicamente via variable de entorno PORT
// Esto permite que la app funcione tanto en local como en cloud
var port = Environment.GetEnvironmentVariable("PORT") ?? "5001";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ====================================================================
// SECCIÓN 1: CONFIGURACIÓN DE BASE DE DATOS
// ====================================================================

// Obtener la cadena de conexión desde appsettings.json
// Si no existe, usa un valor por defecto (Data Source=crm.db)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=crm.db";

// Configurar Entity Framework Core con SQLite
// SQLite es una base de datos en archivo, no requiere instalación de servidor
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// ====================================================================
// SECCIÓN 2: AUTOMAPPER - MAPEO AUTOMÁTICO DE OBJETOS
// ====================================================================

// AutoMapper permite convertir automáticamente entre entidades del dominio y DTOs
// La configuración de los mapeos está en MappingProfile.cs
// Ejemplo: Client → ClientDto, CreateClientDto → Client
builder.Services.AddAutoMapper(typeof(MappingProfile));

// ====================================================================
// SECCIÓN 3: HTTPCONTEXTACCESSOR - ACCESO AL CONTEXTO HTTP
// ====================================================================

// Permite acceder al HttpContext desde servicios/repositorios
// Se usa en AppDbContext para obtener el usuario autenticado (JWT)
// y registrar automáticamente CreatedBy/UpdatedBy en auditoría
builder.Services.AddHttpContextAccessor();

// ====================================================================
// SECCIÓN 4: AUTENTICACIÓN JWT (JSON WEB TOKENS)
// ====================================================================

// Obtener configuración de JWT desde appsettings.json
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];

// Configurar autenticación basada en JWT
// Los usuarios hacen login y reciben un token que deben incluir en cada request
builder.Services.AddAuthentication(options =>
{
    // Establecer JWT Bearer como el esquema de autenticación por defecto
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configurar cómo validar los tokens JWT
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,              // Validar quién emitió el token
        ValidateAudience = true,            // Validar para quién es el token
        ValidateLifetime = true,            // Validar que el token no haya expirado
        ValidateIssuerSigningKey = true,    // Validar la firma del token
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

// ====================================================================
// SECCIÓN 5: POLÍTICAS DE AUTORIZACIÓN
// ====================================================================

// Configurar políticas de autorización basadas en roles
// Las políticas permiten definir requisitos de acceso reutilizables
builder.Services.AddAuthorization(options =>
{
    // Política: Solo Admin puede ejecutar la acción
    // Uso en controller: [Authorize(Policy = "AdminOnly")]
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    // Política: Admin o Asesor pueden ejecutar la acción
    // Uso: [Authorize(Policy = "AdminOrAsesor")]
    options.AddPolicy("AdminOrAsesor", policy => 
        policy.RequireRole("Admin", "Asesor"));
    
    // Política: Cualquier usuario autenticado con rol válido
    // Uso: [Authorize(Policy = "AllRoles")]
    options.AddPolicy("AllRoles", policy => 
        policy.RequireRole("Admin", "Asesor", "Auditor"));
    
    // Política: Solo Admin y Asesor pueden crear/modificar clientes
    // Uso: [Authorize(Policy = "ClientManagement")]
    options.AddPolicy("ClientManagement", policy => 
        policy.RequireRole("Admin", "Asesor"));
    
    // Política: Admin, Asesor y Auditor pueden leer (solo consultar)
    // Uso: [Authorize(Policy = "ReadOnly")]
    options.AddPolicy("ReadOnly", policy => 
        policy.RequireRole("Admin", "Asesor", "Auditor"));
    
    // Política: Admin y Asesor pueden gestionar notas
    // Los Asesores documentan interacciones, es su función principal
    // Uso: [Authorize(Policy = "NoteManagement")]
    options.AddPolicy("NoteManagement", policy => 
        policy.RequireRole("Admin", "Asesor"));
    
    // Política: Solo Admin puede eliminar registros (soft delete)
    // Uso: [Authorize(Policy = "DeletePermission")]
    options.AddPolicy("DeletePermission", policy => 
        policy.RequireRole("Admin"));
});

// ====================================================================
// SECCIÓN 6: CORS - COMPARTIR RECURSOS ENTRE ORÍGENES
// ====================================================================

// Configurar CORS para permitir requests desde el frontend Angular
// Esto es necesario porque el frontend (localhost:4200) y el backend (Railway)
// están en diferentes dominios, y los navegadores bloquean esto por seguridad
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",      // Angular development
                "http://localhost:4201",      // Backup port
                "https://localhost:4200",     // HTTPS local
                "https://localhost:4201"      // HTTPS backup
            )
            .AllowAnyMethod()                 // Permitir GET, POST, PUT, DELETE, etc.
            .AllowAnyHeader()                 // Permitir cualquier header (Authorization, Content-Type, etc.)
            .AllowCredentials();              // Permitir cookies y credenciales
    });
});

// ====================================================================
// SECCIÓN 7: CONTROLADORES Y API
// ====================================================================

// Registrar los controladores de la API
builder.Services.AddControllers();

// Registrar la generación de documentación de API (OpenAPI/Swagger)
builder.Services.AddEndpointsApiExplorer();

// ====================================================================
// SECCIÓN 7: SWAGGER UI - DOCUMENTACIÓN INTERACTIVA
// ====================================================================

// Configurar Swagger para documentación y pruebas de la API
builder.Services.AddSwaggerGen(c =>
{
    // Información general de la API
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "CRM Service API", 
        Version = "v1",
        Description = "API REST para gestión de relaciones con clientes (CRM)"
    });
    
    // ----------------------------------------------------------------
    // Configuración de JWT en Swagger
    // ----------------------------------------------------------------
    // Esto agrega un botón "Authorize" en la UI de Swagger
    // donde el usuario puede ingresar su token JWT
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Requerir el token JWT para todos los endpoints en Swagger
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ====================================================================
// SECCIÓN 8: INYECCIÓN DE DEPENDENCIAS - REPOSITORIOS Y SERVICIOS
// ====================================================================

// Registrar repositorios (capa de acceso a datos)
// Patrón: AddScoped crea una instancia por request HTTP
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IOpportunityRepository, OpportunityRepository>();

// Registrar servicios (capa de lógica de negocio)
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ====================================================================
// CONSTRUCCIÓN Y CONFIGURACIÓN DEL PIPELINE DE LA APLICACIÓN
// ====================================================================

var app = builder.Build();

// ====================================================================
// INICIALIZACIÓN DE BASE DE DATOS Y SEEDING
// ====================================================================

// Crear un scope temporal para acceder a los servicios registrados
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		// Obtener el contexto de base de datos
		var context = services.GetRequiredService<AppDbContext>();
		
		// Asegurar que la base de datos existe y está actualizada
		// Esto aplicará las migraciones pendientes automáticamente
		Console.WriteLine("🔄 Verificando/Creando base de datos...");
		context.Database.EnsureCreated();
		Console.WriteLine("✅ Base de datos lista.");
		
		// Ejecutar el seeder para datos iniciales
		DbSeeder.SeedData(context);
	}
	catch (Exception ex)
	{
		// Registrar el error si falla la inicialización
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "❌ Error al inicializar la base de datos o ejecutar el seeding.");
	}
}

// ====================================================================
// MIDDLEWARE 1: MANEJO CENTRALIZADO DE ERRORES
// ====================================================================

// Este middleware debe ir PRIMERO para capturar todas las excepciones
// Convierte excepciones en respuestas HTTP apropiadas (400, 404, 500, etc.)
app.UseMiddleware<ErrorHandlingMiddleware>();

// ====================================================================
// MIDDLEWARE 2: SWAGGER
// ====================================================================

// Habilitar Swagger UI para demos y documentación interactiva
// NOTA: En un entorno real de producción, considera deshabilitar o proteger Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CRM API v1");
    c.RoutePrefix = "swagger"; // Accesible en /swagger
});

// ====================================================================
// MIDDLEWARE 3: REDIRECCIÓN HTTPS
// ====================================================================

// Redirigir automáticamente todas las peticiones HTTP a HTTPS
app.UseHttpsRedirection();

// ====================================================================
// MIDDLEWARE 4: CORS - PERMITIR REQUESTS DESDE FRONTEND
// ====================================================================

// Habilitar la política CORS definida anteriormente
// Debe ir ANTES de UseAuthentication y UseAuthorization
app.UseCors("AllowAngularApp");

// ====================================================================
// MIDDLEWARE 5: AUTENTICACIÓN Y AUTORIZACIÓN
// ====================================================================

// ORDEN IMPORTANTE: UseAuthentication debe ir ANTES de UseAuthorization

// UseAuthentication: Lee el token JWT del header y valida al usuario
app.UseAuthentication();

// UseAuthorization: Verifica que el usuario tenga permisos para el endpoint
// (usando atributos [Authorize] y [Authorize(Roles = "...")])
app.UseAuthorization();

// ====================================================================
// MIDDLEWARE 6: MAPEO DE CONTROLADORES
// ====================================================================

// Mapear los endpoints de los controladores
app.MapControllers();

// ====================================================================
// ARRANCAR LA APLICACIÓN
// ====================================================================

app.Run();
