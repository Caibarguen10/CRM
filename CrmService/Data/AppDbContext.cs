using CrmService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrmService.Data;

/// <summary>
/// Contexto de base de datos de Entity Framework Core para el sistema CRM.
/// Gestiona todas las entidades del dominio y proporciona funcionalidades avanzadas:
/// - Filtros globales de soft delete
/// - Auditoría automática de cambios
/// - Configuración de relaciones y restricciones
/// </summary>
public class AppDbContext : DbContext
{
	/// <summary>
	/// Proveedor de acceso al contexto HTTP para obtener información del usuario autenticado.
	/// Se usa para registrar automáticamente quién crea/modifica/elimina registros.
	/// Puede ser null en contextos sin HTTP (ej: migraciones, seeds).
	/// </summary>
	private readonly IHttpContextAccessor? _httpContextAccessor;

	/// <summary>
	/// Constructor del contexto de base de datos.
	/// </summary>
	/// <param name="options">Opciones de configuración del DbContext (provider, connection string, etc.)</param>
	/// <param name="httpContextAccessor">Opcional: Proveedor de contexto HTTP para auditoría</param>
	public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	#region DbSets - Tablas de la Base de Datos
	
	/// <summary>
	/// Conjunto de entidades Client (Clientes).
	/// Tabla principal del CRM.
	/// </summary>
	public DbSet<Client> Clients => Set<Client>();
	
	/// <summary>
	/// Conjunto de entidades Contact (Contactos).
	/// Personas de contacto asociadas a clientes.
	/// </summary>
	public DbSet<Contact> Contacts => Set<Contact>();
	
	/// <summary>
	/// Conjunto de entidades ClientNote (Notas de Clientes).
	/// Historial de interacciones con los clientes.
	/// </summary>
	public DbSet<ClientNote> ClientNotes => Set<ClientNote>();
	
	/// <summary>
	/// Conjunto de entidades Opportunity (Oportunidades de Negocio).
	/// Pipeline de ventas asociadas a clientes.
	/// </summary>
	public DbSet<Opportunity> Opportunities => Set<Opportunity>();
	
	/// <summary>
	/// Conjunto de entidades User (Usuarios del Sistema).
	/// Gestión de autenticación y autorización.
	/// </summary>
	public DbSet<User> Users => Set<User>();
	
	#endregion

	/// <summary>
	/// Configura el modelo de datos, relaciones y restricciones.
	/// Se ejecuta una vez al crear el modelo de EF Core.
	/// </summary>
	/// <param name="modelBuilder">Constructor del modelo de EF Core</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		#region Filtros Globales de Soft Delete
		
		// Estos filtros hacen que todas las consultas LINQ excluyan automáticamente
		// los registros marcados como eliminados (IsDeleted = true).
		// Para incluir registros eliminados, usar: .IgnoreQueryFilters()
		
		modelBuilder.Entity<Client>().HasQueryFilter(e => !e.IsDeleted);
		modelBuilder.Entity<Contact>().HasQueryFilter(e => !e.IsDeleted);
		modelBuilder.Entity<ClientNote>().HasQueryFilter(e => !e.IsDeleted);
		modelBuilder.Entity<Opportunity>().HasQueryFilter(e => !e.IsDeleted);
		modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
		
		#endregion

		#region Configuración de Entidad: Client (Cliente)
		
		modelBuilder.Entity<Client>(entity =>
		{
			// Configuración de clave primaria
			entity.HasKey(x => x.Id);
			
			// Configuración de propiedades
			entity.Property(x => x.DocumentNumber).HasMaxLength(50).IsRequired();
			entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
			entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
			entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
			
			// Índice único: No puede haber dos clientes con el mismo número de documento
			entity.HasIndex(x => x.DocumentNumber).IsUnique();
		});
		
		#endregion

		#region Configuración de Entidad: Contact (Contacto)
		
		modelBuilder.Entity<Contact>(entity =>
		{
			// Configuración de clave primaria
			entity.HasKey(x => x.Id);
			
			// Configuración de propiedades
			entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
			entity.Property(x => x.Position).HasMaxLength(100).IsRequired();
			entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
			entity.Property(x => x.Phone).HasMaxLength(30).IsRequired();
			
			// Relación: Un contacto pertenece a un cliente (muchos a uno)
			// DeleteBehavior.Cascade: Si se elimina el cliente, se eliminan sus contactos
			entity.HasOne(x => x.Client)
				.WithMany(x => x.Contacts)
				.HasForeignKey(x => x.ClientId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		#endregion

		#region Configuración de Entidad: ClientNote (Nota de Cliente)
		
		modelBuilder.Entity<ClientNote>(entity =>
		{
			// Configuración de clave primaria
			entity.HasKey(x => x.Id);
			
			// Configuración de propiedades
			entity.Property(x => x.Note).HasMaxLength(1000).IsRequired();
			
			// Relación: Una nota pertenece a un cliente (muchos a uno)
			// DeleteBehavior.Cascade: Si se elimina el cliente, se eliminan sus notas
			entity.HasOne(x => x.Client)
				.WithMany(x => x.Notes)
				.HasForeignKey(x => x.ClientId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		#endregion

		#region Configuración de Entidad: Opportunity (Oportunidad)
		
		modelBuilder.Entity<Opportunity>(entity =>
		{
			// Configuración de clave primaria
			entity.HasKey(x => x.Id);
			
			// Configuración de propiedades
			entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
			entity.Property(x => x.Status).HasMaxLength(50).IsRequired();
			
			// Precisión decimal: 18 dígitos totales, 2 decimales
			// Ejemplo: 9999999999999999.99
			entity.Property(x => x.EstimatedAmount).HasPrecision(18, 2);
			
			// Relación: Una oportunidad pertenece a un cliente (muchos a uno)
			// DeleteBehavior.Cascade: Si se elimina el cliente, se eliminan sus oportunidades
			entity.HasOne(x => x.Client)
				.WithMany(x => x.Opportunities)
				.HasForeignKey(x => x.ClientId)
				.OnDelete(DeleteBehavior.Cascade);
		});
		
		#endregion

		#region Configuración de Entidad: User (Usuario)
		
		modelBuilder.Entity<User>(entity =>
		{
			// Configuración de clave primaria
			entity.HasKey(x => x.Id);
			
			// Configuración de propiedades
			entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
			entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
			
			// El enum UserRole se convierte a string en la base de datos
			// En lugar de guardar 1,2,3 se guarda "Admin", "Asesor", "Auditor"
			entity.Property(x => x.Role).HasConversion<string>().IsRequired();
			
			// Índices únicos: No puede haber usuarios duplicados
			entity.HasIndex(x => x.Username).IsUnique();
			entity.HasIndex(x => x.Email).IsUnique();
		});
		
		#endregion
	}

	#region Sobrescritura de SaveChanges para Auditoría Automática

	/// <summary>
	/// Guarda los cambios de manera asíncrona aplicando auditoría automática.
	/// Antes de guardar, procesa los campos de auditoría (CreatedBy, UpdatedBy, etc.).
	/// </summary>
	/// <param name="cancellationToken">Token de cancelación para operaciones asíncronas</param>
	/// <returns>Número de registros afectados</returns>
	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		ProcessAuditFields();
		return base.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// Guarda los cambios de manera síncrona aplicando auditoría automática.
	/// Antes de guardar, procesa los campos de auditoría (CreatedBy, UpdatedBy, etc.).
	/// </summary>
	/// <returns>Número de registros afectados</returns>
	public override int SaveChanges()
	{
		ProcessAuditFields();
		return base.SaveChanges();
	}

	/// <summary>
	/// Procesa automáticamente los campos de auditoría y soft delete
	/// para todas las entidades que heredan de BaseEntity.
	/// </summary>
	/// <remarks>
	/// Este método se ejecuta antes de cada SaveChanges y:
	/// 
	/// 1. Para registros NUEVOS (Added):
	///    - Asigna CreatedAt = DateTime.UtcNow
	///    - Asigna CreatedBy = usuario del token JWT (o "System")
	///    - Marca IsDeleted = false
	/// 
	/// 2. Para registros MODIFICADOS (Modified):
	///    - Si IsDeleted = true: Asigna DeletedAt y DeletedBy (soft delete)
	///    - Si no: Asigna UpdatedAt y UpdatedBy
	/// 
	/// 3. Para registros ELIMINADOS (Deleted):
	///    - Intercepta el DELETE físico
	///    - Lo convierte en UPDATE con IsDeleted = true (soft delete)
	///    - Asigna DeletedAt y DeletedBy
	/// </remarks>
	private void ProcessAuditFields()
	{
		// Obtener el nombre del usuario autenticado desde el token JWT
		// Si no hay usuario autenticado (ej: migraciones), usar "System"
		var currentUsername = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "System";
		
		// Obtener todas las entidades rastreadas que heredan de BaseEntity
		var entries = ChangeTracker.Entries<BaseEntity>();

		foreach (var entry in entries)
		{
			switch (entry.State)
			{
				case EntityState.Added:
					// Nuevo registro: Asignar campos de creación
					entry.Entity.CreatedAt = DateTime.UtcNow;
					entry.Entity.CreatedBy = currentUsername;
					entry.Entity.IsDeleted = false;
					break;

				case EntityState.Modified:
					if (entry.Entity.IsDeleted)
					{
						// Soft Delete: El registro fue marcado como eliminado
						entry.Entity.DeletedAt = DateTime.UtcNow;
						entry.Entity.DeletedBy = currentUsername;
					}
					else
					{
						// Actualización normal: Asignar campos de modificación
						entry.Entity.UpdatedAt = DateTime.UtcNow;
						entry.Entity.UpdatedBy = currentUsername;
					}
					break;

				case EntityState.Deleted:
					// Interceptar DELETE físico y convertirlo en Soft Delete
					// Cambiamos el estado a Modified en lugar de Deleted
					entry.State = EntityState.Modified;
					entry.Entity.IsDeleted = true;
					entry.Entity.DeletedAt = DateTime.UtcNow;
					entry.Entity.DeletedBy = currentUsername;
					break;
			}
		}
	}
	
	#endregion
}
