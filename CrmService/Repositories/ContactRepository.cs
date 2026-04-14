using CrmService.Data;
using CrmService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrmService.Repositories;

/// <summary>
/// Implementación del repositorio de contactos que gestiona el acceso a datos
/// de la entidad Contact usando Entity Framework Core.
/// </summary>
/// <remarks>
/// ARQUITECTURA:
/// ------------------------------------------------------------------------
/// Esta clase implementa el patrón Repository, abstrayendo EF Core de la capa
/// de servicios. Todas las operaciones de base de datos relacionadas con contactos
/// están centralizadas aquí.
/// 
/// CARACTERÍSTICAS TÉCNICAS:
/// ------------------------------------------------------------------------
/// 1. INYECCIÓN DE DEPENDENCIAS:
///    - Recibe AppDbContext por constructor
///    - El ciclo de vida del DbContext es Scoped (una instancia por request HTTP)
/// 
/// 2. OPTIMIZACIONES:
///    - AsNoTracking() en operaciones de solo lectura (mejor rendimiento)
///    - AnyAsync() para validaciones (más eficiente que Count o FirstOrDefault)
/// 
/// 3. AUDITORÍA AUTOMÁTICA:
///    - SaveChangesAsync() dispara ProcessAuditFields() en AppDbContext
///    - Los campos CreatedBy, CreatedAt se populan automáticamente
/// 
/// 4. SOFT DELETE:
///    - Los filtros globales excluyen registros con IsDeleted = true
///    - NO es necesario agregar .Where(x => !x.IsDeleted) manualmente
/// 
/// RELACIÓN CON OTRAS CAPAS:
/// ------------------------------------------------------------------------
/// ContactService → ContactRepository → AppDbContext → SQLite Database
/// 
/// PERMISOS (verificados en capa superior - Controlador/Servicio):
/// ------------------------------------------------------------------------
/// - Admin: Puede crear/leer contactos de cualquier cliente
/// - Asesor: Puede leer contactos, agregar notas a contactos
/// - Auditor: Solo puede leer contactos
/// </remarks>
public class ContactRepository : IContactRepository
{
	/// <summary>
	/// Contexto de base de datos de Entity Framework Core.
	/// </summary>
	/// <remarks>
	/// Este contexto:
	/// - Gestiona la conexión a SQLite (archivo crm.db)
	/// - Intercepta SaveChanges para aplicar auditoría automática
	/// - Aplica filtros globales de soft delete en todas las queries
	/// - Tiene configuradas las relaciones entre entidades (Client → Contact)
	/// </remarks>
	private readonly AppDbContext _context;

	/// <summary>
	/// Constructor que recibe el contexto de base de datos por inyección de dependencias.
	/// </summary>
	/// <param name="context">Contexto de EF Core (Scoped lifetime)</param>
	/// <remarks>
	/// CICLO DE VIDA:
	/// - El DbContext se crea al inicio de cada request HTTP
	/// - Se destruye automáticamente al finalizar el request
	/// - NO se debe reutilizar entre requests (evita problemas de concurrencia)
	/// </remarks>
	public ContactRepository(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Obtiene todos los contactos asociados a un cliente específico, ordenados alfabéticamente.
	/// </summary>
	/// <param name="clientId">ID del cliente del cual obtener los contactos</param>
	/// <returns>Lista de contactos del cliente, vacía si no tiene contactos</returns>
	/// <remarks>
	/// PROCESO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. _context.Contacts: Accede al DbSet de contactos
	/// 2. AsNoTracking(): Desactiva change tracking (solo lectura, mejor performance)
	/// 3. Where(x => x.ClientId == clientId): Filtra por cliente
	/// 4. (AUTOMÁTICO) Filtro global IsDeleted = false se aplica implícitamente
	/// 5. OrderBy(x => x.Name): Ordena alfabéticamente por nombre
	/// 6. ToListAsync(): Ejecuta la query y materializa los resultados
	/// 
	/// SQL GENERADO POR EF CORE:
	/// ------------------------------------------------------------------------
	/// SELECT [c].[Id], [c].[Name], [c].[Email], [c].[Phone], [c].[Position],
	///        [c].[ClientId], [c].[CreatedBy], [c].[CreatedAt], [c].[UpdatedBy],
	///        [c].[UpdatedAt], [c].[IsDeleted], [c].[DeletedBy], [c].[DeletedAt]
	/// FROM [Contacts] AS [c]
	/// WHERE [c].[ClientId] = @clientId AND [c].[IsDeleted] = 0
	/// ORDER BY [c].[Name] ASC
	/// 
	/// OPTIMIZACIÓN - AsNoTracking():
	/// ------------------------------------------------------------------------
	/// SIN AsNoTracking() (tracking activado):
	/// - EF Core mantiene una copia de cada entidad en memoria (ChangeTracker)
	/// - Consume más RAM (útil si se van a modificar las entidades)
	/// - Performance: ~1000 contactos = ~5 MB RAM adicional
	/// 
	/// CON AsNoTracking() (tracking desactivado):
	/// - EF Core NO mantiene copia en ChangeTracker
	/// - Menor consumo de RAM (30-40% menos)
	/// - Más rápido en queries de solo lectura
	/// - NO se pueden modificar y guardar estas entidades después
	/// 
	/// CASOS DE USO:
	/// ------------------------------------------------------------------------
	/// Escenario 1 - Obtener contactos de un cliente:
	///   var contacts = await GetByClientIdAsync(10);
	///   // Retorna: [Contact{Name="Ana López"}, Contact{Name="Carlos Díaz"}]
	/// 
	/// Escenario 2 - Cliente sin contactos:
	///   var contacts = await GetByClientIdAsync(99);
	///   // Retorna: [] (lista vacía, NO null)
	/// 
	/// Escenario 3 - Cliente con contactos eliminados:
	///   // Cliente tiene 3 contactos, pero 1 está eliminado (IsDeleted=true)
	///   var contacts = await GetByClientIdAsync(5);
	///   // Retorna solo 2 contactos (el eliminado NO aparece)
	/// 
	/// VENTAJAS DE ORDENAR POR NOMBRE:
	/// ------------------------------------------------------------------------
	/// - Facilita la búsqueda visual al usuario en UI
	/// - Consistencia en la presentación de datos
	/// - Mejor UX en listas largas de contactos
	/// </remarks>
	public async Task<List<Contact>> GetByClientIdAsync(int clientId)
	{
		return await _context.Contacts
			.AsNoTracking() // Solo lectura, mejor performance
			.Where(x => x.ClientId == clientId) // Filtra por cliente
			.OrderBy(x => x.Name) // Orden alfabético A-Z
			.ToListAsync(); // Ejecuta query y retorna lista
	}

	/// <summary>
	/// Crea un nuevo contacto asociado a un cliente.
	/// </summary>
	/// <param name="contact">Entidad contacto con los datos a guardar</param>
	/// <returns>El contacto creado con su ID asignado y campos de auditoría populados</returns>
	/// <remarks>
	/// PROCESO COMPLETO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. _context.Contacts.Add(contact): Marca la entidad como "Added" en ChangeTracker
	/// 2. await _context.SaveChangesAsync(): Dispara el guardado
	/// 3. (INTERNO) AppDbContext.SaveChangesAsync() intercepta la operación
	/// 4. (INTERNO) ProcessAuditFields() detecta estado "Added"
	/// 5. (INTERNO) Asigna automáticamente:
	///    - contact.CreatedBy = HttpContext.User.Identity.Name (del JWT)
	///    - contact.CreatedAt = DateTime.UtcNow (hora actual UTC)
	/// 6. (INTERNO) EF Core genera el SQL INSERT
	/// 7. (INTERNO) SQLite ejecuta el INSERT y retorna el nuevo ID
	/// 8. (INTERNO) EF Core actualiza contact.Id con el valor generado
	/// 9. return contact: Retorna el contacto con ID y auditoría poblados
	/// 
	/// SQL GENERADO POR EF CORE:
	/// ------------------------------------------------------------------------
	/// INSERT INTO [Contacts] (
	///     [Name], [Email], [Phone], [Position], [ClientId],
	///     [CreatedBy], [CreatedAt], [IsDeleted]
	/// )
	/// VALUES (
	///     @p0, @p1, @p2, @p3, @p4,
	///     @p5, @p6, 0
	/// );
	/// SELECT [Id] FROM [Contacts] WHERE [rowid] = last_insert_rowid();
	/// 
	/// PARÁMETROS SQL EJEMPLO:
	/// ------------------------------------------------------------------------
	/// @p0 = 'María Fernández'        (Name)
	/// @p1 = 'maria@empresa.com'      (Email)
	/// @p2 = '+34 600 123 456'        (Phone)
	/// @p3 = 'Gerente de Operaciones' (Position)
	/// @p4 = 15                        (ClientId)
	/// @p5 = 'admin@crm.com'          (CreatedBy - automático)
	/// @p6 = '2026-04-14T15:45:30Z'   (CreatedAt - automático)
	/// 
	/// AUDITORÍA AUTOMÁTICA - CAMPOS POBLADOS:
	/// ------------------------------------------------------------------------
	/// ANTES de SaveChangesAsync():
	/// {
	///     Id = 0,
	///     Name = "María Fernández",
	///     Email = "maria@empresa.com",
	///     ClientId = 15,
	///     CreatedBy = null,        ← Vacío
	///     CreatedAt = default,     ← Vacío
	///     IsDeleted = false
	/// }
	/// 
	/// DESPUÉS de SaveChangesAsync():
	/// {
	///     Id = 42,                              ← Asignado por BD
	///     Name = "María Fernández",
	///     Email = "maria@empresa.com",
	///     ClientId = 15,
	///     CreatedBy = "admin@crm.com",         ← Automático (del JWT)
	///     CreatedAt = 2026-04-14T15:45:30Z,    ← Automático (UTC)
	///     IsDeleted = false
	/// }
	/// 
	/// VALIDACIONES REQUERIDAS (en ContactService, NO aquí):
	/// ------------------------------------------------------------------------
	/// - ClientId debe existir (usar ClientExistsAsync)
	/// - Email debe tener formato válido
	/// - Name y Email son obligatorios
	/// - Phone debe tener formato válido si se proporciona
	/// 
	/// NOTA: Este repositorio NO valida datos, solo persiste. Las validaciones
	/// se hacen en ContactService usando DTOs con DataAnnotations.
	/// 
	/// EJEMPLO DE USO DESDE SERVICIO:
	/// ------------------------------------------------------------------------
	/// // 1. Validar que el cliente existe
	/// if (!await _contactRepo.ClientExistsAsync(dto.ClientId))
	///     throw new NotFoundException("Cliente no encontrado");
	/// 
	/// // 2. Mapear DTO a entidad
	/// var contact = new Contact 
	/// { 
	///     Name = dto.Name,
	///     Email = dto.Email,
	///     Phone = dto.Phone,
	///     Position = dto.Position,
	///     ClientId = dto.ClientId
	/// };
	/// 
	/// // 3. Crear en base de datos (auditoría automática)
	/// var created = await _contactRepo.CreateAsync(contact);
	/// 
	/// // 4. Retornar DTO con ID asignado
	/// return _mapper.Map<ContactDto>(created);
	/// 
	/// TRANSACCIONALIDAD:
	/// ------------------------------------------------------------------------
	/// - SaveChangesAsync() es transaccional (ACID)
	/// - Si falla, hace rollback automático
	/// - Si tiene éxito, el commit es inmediato
	/// </remarks>
	public async Task<Contact> CreateAsync(Contact contact)
	{
		_context.Contacts.Add(contact); // Marca como "Added" en ChangeTracker
		await _context.SaveChangesAsync(); // Ejecuta INSERT + auditoría automática
		return contact; // Retorna con ID asignado
	}

	/// <summary>
	/// Verifica si existe un cliente con el ID especificado (sin estar eliminado).
	/// </summary>
	/// <param name="clientId">ID del cliente a verificar</param>
	/// <returns>true si el cliente existe y NO está eliminado, false en caso contrario</returns>
	/// <remarks>
	/// PROCESO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. _context.Clients: Accede al DbSet de clientes
	/// 2. (AUTOMÁTICO) Filtro global IsDeleted = false se aplica implícitamente
	/// 3. AnyAsync(x => x.Id == clientId): Verifica existencia
	/// 4. Retorna true si encuentra coincidencia, false si no
	/// 
	/// SQL GENERADO POR EF CORE:
	/// ------------------------------------------------------------------------
	/// SELECT CASE
	///     WHEN EXISTS (
	///         SELECT 1 FROM [Clients] AS [c]
	///         WHERE [c].[Id] = @clientId AND [c].[IsDeleted] = 0
	///     )
	///     THEN CAST(1 AS bit)
	///     ELSE CAST(0 AS bit)
	/// END AS [Value]
	/// 
	/// OPTIMIZACIÓN - AnyAsync() vs CountAsync() vs FirstOrDefaultAsync():
	/// ------------------------------------------------------------------------
	/// AnyAsync() (RECOMENDADO para validación):
	/// - SQL: SELECT CASE WHEN EXISTS(...) THEN 1 ELSE 0 END
	/// - Performance: Se detiene en cuanto encuentra 1 registro
	/// - Retorna: bool
	/// - Tiempo: ~0.5ms
	/// 
	/// CountAsync() (NO ÓPTIMO):
	/// - SQL: SELECT COUNT(*) FROM ...
	/// - Performance: Cuenta TODOS los registros coincidentes
	/// - Retorna: int
	/// - Tiempo: ~2ms (4x más lento)
	/// 
	/// FirstOrDefaultAsync() (NO ÓPTIMO):
	/// - SQL: SELECT TOP 1 * FROM ...
	/// - Performance: Carga la entidad completa con todas sus columnas
	/// - Retorna: Client? (objeto completo)
	/// - Tiempo: ~1ms (2x más lento)
	/// 
	/// CASOS DE USO:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Cliente existe y está activo:
	///   bool exists = await ClientExistsAsync(10);
	///   // Retorna: true
	/// 
	/// Caso 2 - Cliente NO existe:
	///   bool exists = await ClientExistsAsync(999);
	///   // Retorna: false
	/// 
	/// Caso 3 - Cliente existe pero está eliminado (IsDeleted=true):
	///   bool exists = await ClientExistsAsync(5);
	///   // Retorna: false (el filtro global lo excluye)
	/// 
	/// FLUJO TÍPICO EN SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<ContactDto> CreateContactAsync(CreateContactDto dto)
	/// {
	///     // 1. Validar integridad referencial
	///     if (!await _contactRepo.ClientExistsAsync(dto.ClientId))
	///         throw new NotFoundException($"Cliente con ID {dto.ClientId} no existe");
	///     
	///     // 2. Mapear y crear
	///     var contact = _mapper.Map<Contact>(dto);
	///     var created = await _contactRepo.CreateAsync(contact);
	///     
	///     // 3. Retornar DTO
	///     return _mapper.Map<ContactDto>(created);
	/// }
	/// 
	/// VENTAJAS DE VALIDAR ANTES DE CREAR:
	/// ------------------------------------------------------------------------
	/// SIN validación previa:
	/// - INSERT falla con SqlException: "FOREIGN KEY constraint failed"
	/// - Mensaje de error técnico y poco amigable
	/// - El usuario ve un error 500 Internal Server Error
	/// 
	/// CON validación previa (ClientExistsAsync):
	/// - Se detecta el problema ANTES de intentar el INSERT
	/// - Se lanza NotFoundException con mensaje descriptivo
	/// - El usuario ve un error 404 Not Found con mensaje claro
	/// - Mejor experiencia de usuario (UX)
	/// 
	/// SEGURIDAD:
	/// ------------------------------------------------------------------------
	/// - El filtro global IsDeleted previene acceso a clientes eliminados
	/// - NO se pueden crear contactos para clientes soft-deleted
	/// - Esto mantiene la integridad referencial lógica
	/// </remarks>
	public async Task<bool> ClientExistsAsync(int clientId)
	{
		return await _context.Clients.AnyAsync(x => x.Id == clientId);
	}
}
