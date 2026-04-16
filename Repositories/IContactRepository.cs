using CrmService.Domain;

namespace CrmService.Repositories;

/// <summary>
/// Interfaz del repositorio de contactos que define las operaciones de acceso a datos
/// para la entidad Contact.
/// </summary>
/// <remarks>
/// PROPÓSITO DEL PATRÓN REPOSITORY:
/// ------------------------------------
/// Esta interfaz abstrae la lógica de acceso a datos de EF Core, proporcionando:
/// 
/// 1. SEPARACIÓN DE RESPONSABILIDADES:
///    - La capa de servicios NO conoce detalles de EF Core
///    - Los controladores NO acceden directamente a la base de datos
///    - Facilita cambiar EF Core por otro ORM sin afectar la lógica de negocio
/// 
/// 2. TESTABILIDAD:
///    - Se pueden crear mocks de IContactRepository en tests unitarios
///    - No se necesita base de datos real para probar servicios
/// 
/// 3. REUTILIZACIÓN:
///    - Las mismas operaciones se pueden usar desde múltiples servicios
///    - Evita duplicación de código de acceso a datos
/// 
/// CARACTERÍSTICAS DE LOS CONTACTOS:
/// ------------------------------------
/// - Un contacto PERTENECE a un cliente (relación 1:N)
/// - Se pueden tener múltiples contactos por cliente (Gerente, Asistente, etc.)
/// - Los contactos heredan auditoría automática de BaseEntity
/// - Soft delete: los contactos eliminados NO se borran físicamente
/// 
/// PERMISOS POR ROL:
/// ------------------------------------
/// - Admin: CRUD completo en contactos
/// - Asesor: Read en contactos + CRUD en notas asociadas
/// - Auditor: Solo Read (lectura)
/// </remarks>
public interface IContactRepository
{
	/// <summary>
	/// Obtiene todos los contactos asociados a un cliente específico, ordenados alfabéticamente.
	/// </summary>
	/// <param name="clientId">ID del cliente del cual obtener los contactos</param>
	/// <returns>Lista de contactos del cliente, vacía si no tiene contactos</returns>
	/// <remarks>
	/// COMPORTAMIENTO:
	/// - Retorna solo contactos NO eliminados (IsDeleted = false) gracias al filtro global
	/// - Los resultados se ordenan por nombre alfabéticamente (A-Z)
	/// - Si el cliente no existe, retorna lista vacía (no lanza excepción)
	/// - Si el cliente existe pero no tiene contactos, retorna lista vacía
	/// 
	/// OPTIMIZACIÓN:
	/// - Usa AsNoTracking() para mejor rendimiento (solo lectura)
	/// 
	/// SQL GENERADO APROXIMADO:
	/// SELECT * FROM Contacts 
	/// WHERE ClientId = @clientId AND IsDeleted = 0
	/// ORDER BY Name ASC
	/// 
	/// EJEMPLO DE USO:
	/// var contacts = await _contactRepo.GetByClientIdAsync(5);
	/// // Retorna: [Contact{Name="Juan Pérez"}, Contact{Name="María García"}]
	/// </remarks>
	Task<List<Contact>> GetByClientIdAsync(int clientId);

	/// <summary>
	/// Crea un nuevo contacto asociado a un cliente.
	/// </summary>
	/// <param name="contact">Entidad contacto con los datos a guardar</param>
	/// <returns>El contacto creado con su ID asignado y campos de auditoría populados</returns>
	/// <remarks>
	/// PROCESO AUTOMÁTICO AL GUARDAR (AppDbContext.SaveChangesAsync):
	/// ------------------------------------------------------------------------
	/// 1. EF Core detecta el estado "Added" del contacto
	/// 2. El método ProcessAuditFields() intercepta la operación
	/// 3. Asigna automáticamente:
	///    - CreatedBy = nombre del usuario actual desde JWT token
	///    - CreatedAt = DateTime.UtcNow
	///    - IsDeleted = false (valor por defecto)
	/// 4. EF Core genera el INSERT SQL y ejecuta
	/// 5. Retorna el contacto con su nuevo ID asignado por la base de datos
	/// 
	/// SQL GENERADO APROXIMADO:
	/// INSERT INTO Contacts (Name, Email, Phone, Position, ClientId, CreatedBy, CreatedAt, IsDeleted)
	/// VALUES (@name, @email, @phone, @position, @clientId, @createdBy, @createdAt, 0);
	/// SELECT CAST(SCOPE_IDENTITY() as int); -- Retorna el nuevo ID
	/// 
	/// VALIDACIONES REQUERIDAS (en la capa de servicio):
	/// - El ClientId debe existir (usar ClientExistsAsync)
	/// - Email debe tener formato válido
	/// - Name y Email son obligatorios
	/// 
	/// EJEMPLO DE USO:
	/// var newContact = new Contact 
	/// { 
	///     Name = "Pedro Ramírez", 
	///     Email = "pedro@empresa.com",
	///     ClientId = 10 
	/// };
	/// var saved = await _contactRepo.CreateAsync(newContact);
	/// // saved.Id = 42 (ID asignado)
	/// // saved.CreatedBy = "admin@crm.com" (del JWT)
	/// // saved.CreatedAt = 2026-04-14 15:30:00
	/// </remarks>
	Task<Contact> CreateAsync(Contact contact);

	/// <summary>
	/// Verifica si existe un cliente con el ID especificado (sin estar eliminado).
	/// </summary>
	/// <param name="clientId">ID del cliente a verificar</param>
	/// <returns>true si el cliente existe y NO está eliminado, false en caso contrario</returns>
	/// <remarks>
	/// PROPÓSITO:
	/// - Validar integridad referencial ANTES de crear un contacto
	/// - Evitar crear contactos huérfanos (sin cliente válido)
	/// - Proporcionar mensaje de error descriptivo al usuario
	/// 
	/// COMPORTAMIENTO:
	/// - Respeta el filtro global de soft delete (IsDeleted = false)
	/// - Si el cliente fue eliminado lógicamente, retorna false
	/// - Operación optimizada (AnyAsync es más rápido que CountAsync o FirstOrDefaultAsync)
	/// 
	/// SQL GENERADO APROXIMADO:
	/// SELECT CASE WHEN EXISTS(
	///     SELECT 1 FROM Clients WHERE Id = @clientId AND IsDeleted = 0
	/// ) THEN 1 ELSE 0 END
	/// 
	/// FLUJO TÍPICO EN SERVICIO:
	/// if (!await _contactRepo.ClientExistsAsync(dto.ClientId))
	///     throw new NotFoundException($"Cliente {dto.ClientId} no encontrado");
	/// 
	/// var contact = _mapper.Map<Contact>(dto);
	/// return await _contactRepo.CreateAsync(contact);
	/// 
	/// VENTAJA:
	/// - Evita un DatabaseUpdateException con mensaje críptico de FK violation
	/// - Permite retornar un error 404 descriptivo al usuario
	/// </remarks>
	Task<bool> ClientExistsAsync(int clientId);
}
