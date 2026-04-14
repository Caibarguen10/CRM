using CrmService.Domain;
using CrmService.DTOs;
using CrmService.Common;

namespace CrmService.Repositories;

/// <summary>
/// Interfaz del repositorio de clientes.
/// Define el contrato para operaciones de acceso a datos de clientes.
/// </summary>
/// <remarks>
/// PATRÓN REPOSITORY:
/// 
/// El patrón Repository abstrae la lógica de acceso a datos de la lógica de negocio.
/// 
/// VENTAJAS:
/// 1. Separación de concerns: La capa de servicio no conoce EF Core
/// 2. Testabilidad: Fácil crear mocks de IClientRepository
/// 3. Flexibilidad: Cambiar el ORM sin afectar servicios
/// 4. Reutilización: Mismas queries usadas en múltiples servicios
/// 5. Mantenibilidad: Queries complejas centralizadas
/// 
/// OPERACIONES CRUD:
/// - Create: CreateAsync()
/// - Read: GetAllAsync(), GetByIdAsync(), GetByDocumentAsync()
/// - Update: UpdateAsync()
/// - Delete: DeleteAsync()
/// 
/// CARACTERÍSTICAS ESPECIALES:
/// - Paginación: GetAllAsync() retorna PagedResult
/// - Filtros dinámicos: ClientFilterDto permite búsquedas flexibles
/// - Soft delete automático: DeleteAsync() marca IsDeleted=true (no borra físicamente)
/// </remarks>
public interface IClientRepository
{
	/// <summary>
	/// Obtiene una lista paginada de clientes con filtros opcionales.
	/// </summary>
	/// <param name="pagination">Parámetros de paginación (página y tamaño de página)</param>
	/// <param name="filters">Filtros opcionales de búsqueda (documento, nombre, email)</param>
	/// <returns>Resultado paginado con lista de clientes y metadatos de paginación</returns>
	/// <remarks>
	/// CARACTERÍSTICAS:
	/// - Paginación eficiente con Skip/Take
	/// - Filtros dinámicos por documento, nombre o email
	/// - Retorna total de registros para generar paginación en el frontend
	/// - Usa AsNoTracking() para mejor rendimiento (solo lectura)
	/// 
	/// EJEMPLO DE USO:
	/// var pagination = new PaginationParams { Page = 1, PageSize = 10 };
	/// var filters = new ClientFilterDto { FullName = "Juan" };
	/// var result = await repository.GetAllAsync(pagination, filters);
	/// // result.Items contiene los clientes
	/// // result.TotalCount contiene el total de registros
	/// // result.Page, result.PageSize, result.TotalPages para paginación
	/// </remarks>
	Task<PagedResult<Client>> GetAllAsync(PaginationParams pagination, ClientFilterDto? filters = null);
	
	/// <summary>
	/// Obtiene un cliente por su ID.
	/// </summary>
	/// <param name="id">ID del cliente a buscar</param>
	/// <returns>Cliente encontrado o null si no existe</returns>
	/// <remarks>
	/// NOTA: Solo retorna clientes NO eliminados (IsDeleted = false).
	/// El filtro global de soft delete está activo.
	/// 
	/// Para incluir clientes eliminados usar:
	/// context.Clients.IgnoreQueryFilters().FirstOrDefaultAsync(...)
	/// </remarks>
	Task<Client?> GetByIdAsync(int id);
	
	/// <summary>
	/// Obtiene un cliente por su número de documento.
	/// </summary>
	/// <param name="documentNumber">Número de documento (DNI, RUC, etc.)</param>
	/// <returns>Cliente encontrado o null si no existe</returns>
	/// <remarks>
	/// USOS COMUNES:
	/// 1. Validar que el documento no esté duplicado antes de crear
	/// 2. Buscar cliente por documento en lugar de ID
	/// 
	/// ÍNDICE ÚNICO:
	/// El campo DocumentNumber tiene un índice único en la base de datos,
	/// por lo que esta búsqueda es eficiente.
	/// </remarks>
	Task<Client?> GetByDocumentAsync(string documentNumber);
	
	/// <summary>
	/// Crea un nuevo cliente en la base de datos.
	/// </summary>
	/// <param name="client">Entidad cliente a crear</param>
	/// <returns>Cliente creado con ID asignado</returns>
	/// <remarks>
	/// PROCESO:
	/// 1. EF Core agrega la entidad al contexto
	/// 2. SaveChanges() ejecuta el INSERT
	/// 3. La base de datos genera el ID automáticamente
	/// 4. Los campos de auditoría se llenan automáticamente:
	///    - CreatedAt = DateTime.UtcNow
	///    - CreatedBy = usuario del JWT token
	///    - IsDeleted = false
	/// 
	/// VALIDACIONES PREVIAS RECOMENDADAS:
	/// - Verificar que el DocumentNumber no exista (GetByDocumentAsync)
	/// - Validar formato de email
	/// - Validar que los campos requeridos estén presentes
	/// </remarks>
	Task<Client> CreateAsync(Client client);
	
	/// <summary>
	/// Actualiza un cliente existente.
	/// </summary>
	/// <param name="client">Entidad cliente con los datos actualizados</param>
	/// <returns>Cliente actualizado o null si no existe</returns>
	/// <remarks>
	/// PROCESO:
	/// 1. Busca el cliente por ID en la base de datos
	/// 2. Actualiza las propiedades modificadas
	/// 3. SaveChanges() ejecuta el UPDATE
	/// 4. Los campos de auditoría se actualizan automáticamente:
	///    - UpdatedAt = DateTime.UtcNow
	///    - UpdatedBy = usuario del JWT token
	/// 
	/// IMPORTANTE:
	/// - Si el cliente no existe, retorna null
	/// - Solo se actualizan los campos que se modificaron (change tracking)
	/// - El ID no se puede cambiar
	/// </remarks>
	Task<Client?> UpdateAsync(Client client);
	
	/// <summary>
	/// Elimina un cliente (soft delete).
	/// </summary>
	/// <param name="id">ID del cliente a eliminar</param>
	/// <returns>true si se eliminó exitosamente, false si no existe</returns>
	/// <remarks>
	/// SOFT DELETE:
	/// NO se elimina físicamente el registro de la base de datos.
	/// En su lugar se marca como eliminado:
	/// - IsDeleted = true
	/// - DeletedAt = DateTime.UtcNow
	/// - DeletedBy = usuario del JWT token
	/// 
	/// VENTAJAS DEL SOFT DELETE:
	/// 1. Auditoría: Se sabe quién y cuándo eliminó
	/// 2. Recuperación: Posible restaurar el registro
	/// 3. Integridad: Mantiene relaciones históricas
	/// 4. Cumplimiento: Requerido en muchas regulaciones
	/// 
	/// COMPORTAMIENTO:
	/// - Los clientes eliminados NO aparecen en consultas normales
	/// - El filtro global .HasQueryFilter(e => !e.IsDeleted) los excluye
	/// - Las relaciones (Contacts, Notes, Opportunities) se mantienen
	/// 
	/// ELIMINACIÓN EN CASCADA:
	/// Si se necesita eliminar también los registros relacionados,
	/// se debe hacer manualmente antes de eliminar el cliente.
	/// </remarks>
	Task<bool> DeleteAsync(int id);
}
