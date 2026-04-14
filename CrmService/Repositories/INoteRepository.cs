using CrmService.Domain;

namespace CrmService.Repositories;

/// <summary>
/// Interfaz del repositorio de notas de cliente que define las operaciones de acceso a datos
/// para la entidad ClientNote.
/// </summary>
/// <remarks>
/// PROPÓSITO DE LAS NOTAS EN EL CRM:
/// ------------------------------------
/// Las notas (ClientNote) son registros de interacciones, comentarios o seguimiento
/// asociados a clientes específicos. Casos de uso comunes:
/// 
/// - Llamadas telefónicas: "Cliente solicitó demo del producto"
/// - Reuniones: "Reunión presencial - interesado en plan Enterprise"
/// - Emails importantes: "Envió presupuesto por $50,000 - pendiente respuesta"
/// - Recordatorios: "Llamar el viernes para seguimiento"
/// - Historial de comunicación: Trazabilidad completa de todas las interacciones
/// 
/// ARQUITECTURA DEL PATRÓN REPOSITORY:
/// ------------------------------------
/// Esta interfaz abstrae la lógica de acceso a datos, proporcionando:
/// 
/// 1. SEPARACIÓN DE RESPONSABILIDADES:
///    - NoteService NO conoce detalles de Entity Framework Core
///    - Los controladores NO acceden directamente a AppDbContext
///    - Facilita cambiar el ORM sin afectar la lógica de negocio
/// 
/// 2. TESTABILIDAD:
///    - Se pueden crear mocks de INoteRepository para tests unitarios
///    - No se necesita base de datos real para probar NoteService
///    - Ejemplo: Mock que siempre retorna ClientExistsAsync() = true
/// 
/// 3. REUTILIZACIÓN:
///    - La misma validación ClientExistsAsync() se puede usar desde múltiples servicios
///    - Evita duplicación de código de acceso a datos
/// 
/// CARACTERÍSTICAS DE LAS NOTAS:
/// ------------------------------------
/// - Relación 1:N con Cliente (una nota PERTENECE a un cliente)
/// - Auditoría automática heredada de BaseEntity (CreatedBy, CreatedAt, etc.)
/// - Soft delete: las notas eliminadas NO se borran físicamente de la base de datos
/// - Inmutables después de creación: NO se pueden editar/actualizar (solo crear)
/// 
/// PERMISOS POR ROL (Sistema de Autorización):
/// ------------------------------------
/// - Admin: CRUD completo en notas de cualquier cliente
/// - Asesor: CRUD completo en notas (su tarea principal es documentar interacciones)
/// - Auditor: Solo Read (lectura) - puede ver historial pero NO modificar
/// 
/// NOTA IMPORTANTE:
/// La verificación de permisos se hace en la capa de Controlador/Servicio,
/// NO en el repositorio. El repositorio solo ejecuta operaciones de datos.
/// </remarks>
public interface INoteRepository
{
	/// <summary>
	/// Crea una nueva nota asociada a un cliente.
	/// </summary>
	/// <param name="note">Entidad nota con el contenido a guardar</param>
	/// <returns>La nota creada con su ID asignado y campos de auditoría populados</returns>
	/// <remarks>
	/// PROCESO AUTOMÁTICO AL GUARDAR (AppDbContext.SaveChangesAsync):
	/// ------------------------------------------------------------------------
	/// 1. EF Core detecta el estado "Added" de la nota
	/// 2. El método ProcessAuditFields() en AppDbContext intercepta la operación
	/// 3. Asigna automáticamente:
	///    - CreatedBy = nombre del usuario actual obtenido del JWT token
	///    - CreatedAt = DateTime.UtcNow (hora actual en formato UTC)
	///    - IsDeleted = false (valor por defecto)
	/// 4. EF Core genera el INSERT SQL y ejecuta contra SQLite
	/// 5. SQLite retorna el nuevo ID autogenerado (autoincremental)
	/// 6. EF Core actualiza note.Id con el valor retornado
	/// 7. Retorna la nota con su ID asignado y auditoría completa
	/// 
	/// SQL GENERADO APROXIMADO:
	/// INSERT INTO ClientNotes (Content, ClientId, CreatedBy, CreatedAt, IsDeleted)
	/// VALUES (@content, @clientId, @createdBy, @createdAt, 0);
	/// SELECT last_insert_rowid(); -- Retorna el nuevo ID
	/// 
	/// VALIDACIONES REQUERIDAS (en NoteService, NO aquí):
	/// - El ClientId debe existir en la tabla Clients (usar ClientExistsAsync)
	/// - El campo Content NO debe estar vacío (mínimo 10 caracteres recomendado)
	/// - El campo Content tiene un máximo de 5000 caracteres
	/// 
	/// EJEMPLO DE USO:
	/// var newNote = new ClientNote 
	/// { 
	///     Content = "Cliente solicitó información de precios. Enviar catálogo.",
	///     ClientId = 15 
	/// };
	/// var saved = await _noteRepo.CreateAsync(newNote);
	/// // saved.Id = 128 (ID asignado por SQLite)
	/// // saved.CreatedBy = "asesor@crm.com" (extraído del JWT token)
	/// // saved.CreatedAt = 2026-04-14 16:15:00 UTC
	/// 
	/// TRAZABILIDAD Y AUDITORÍA:
	/// ------------------------------------------------------------------------
	/// Cada nota queda registrada con:
	/// - Quién la creó (CreatedBy) → Usuario desde JWT
	/// - Cuándo la creó (CreatedAt) → Timestamp UTC
	/// - A qué cliente pertenece (ClientId) → Foreign Key
	/// 
	/// Esto proporciona un historial completo e inmutable de todas las interacciones
	/// con cada cliente, esencial para CRM y auditorías posteriores.
	/// </remarks>
	Task<ClientNote> CreateAsync(ClientNote note);

	/// <summary>
	/// Verifica si existe un cliente con el ID especificado (sin estar eliminado).
	/// </summary>
	/// <param name="clientId">ID del cliente a verificar</param>
	/// <returns>true si el cliente existe y NO está eliminado, false en caso contrario</returns>
	/// <remarks>
	/// PROPÓSITO:
	/// - Validar integridad referencial ANTES de crear una nota
	/// - Evitar crear notas huérfanas (asociadas a clientes inexistentes o eliminados)
	/// - Proporcionar mensajes de error descriptivos al usuario
	/// 
	/// COMPORTAMIENTO DEL FILTRO GLOBAL:
	/// - Respeta el filtro global de soft delete configurado en AppDbContext
	/// - Si el cliente fue eliminado lógicamente (IsDeleted = true), retorna false
	/// - Esto previene crear notas para clientes que ya no están activos en el sistema
	/// 
	/// OPTIMIZACIÓN:
	/// - Usa AnyAsync() en lugar de CountAsync() o FirstOrDefaultAsync()
	/// - AnyAsync() es más eficiente porque se detiene apenas encuentra 1 registro
	/// - No carga datos innecesarios, solo verifica existencia
	/// 
	/// SQL GENERADO APROXIMADO:
	/// SELECT CASE WHEN EXISTS(
	///     SELECT 1 FROM Clients WHERE Id = @clientId AND IsDeleted = 0
	/// ) THEN 1 ELSE 0 END
	/// 
	/// FLUJO TÍPICO EN SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<NoteDto> CreateNoteAsync(CreateNoteDto dto)
	/// {
	///     // 1. Validar que el cliente existe
	///     if (!await _noteRepo.ClientExistsAsync(dto.ClientId))
	///         throw new NotFoundException($"Cliente con ID {dto.ClientId} no encontrado");
	///     
	///     // 2. Mapear DTO a entidad
	///     var note = _mapper.Map<ClientNote>(dto);
	///     
	///     // 3. Crear en base de datos (auditoría automática)
	///     var created = await _noteRepo.CreateAsync(note);
	///     
	///     // 4. Retornar DTO con ID asignado
	///     return _mapper.Map<NoteDto>(created);
	/// }
	/// 
	/// VENTAJA SOBRE NO VALIDAR:
	/// ------------------------------------------------------------------------
	/// SIN validación previa:
	/// - El INSERT falla con SqlException: "FOREIGN KEY constraint failed"
	/// - Mensaje técnico y críptico para el usuario
	/// - Se retorna un error 500 Internal Server Error
	/// 
	/// CON validación previa usando ClientExistsAsync():
	/// - Se detecta el problema ANTES de intentar el INSERT
	/// - Se lanza NotFoundException con mensaje en español: "Cliente X no encontrado"
	/// - Se retorna un error 404 Not Found descriptivo
	/// - Mejor experiencia de usuario (UX) y debugging
	/// 
	/// CASOS DE USO:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Cliente existe y está activo:
	///   bool exists = await ClientExistsAsync(10);
	///   // Retorna: true → Permitir creación de la nota
	/// 
	/// Caso 2 - Cliente NO existe:
	///   bool exists = await ClientExistsAsync(999);
	///   // Retorna: false → Lanzar NotFoundException
	/// 
	/// Caso 3 - Cliente eliminado (IsDeleted = true):
	///   bool exists = await ClientExistsAsync(5);
	///   // Retorna: false → El filtro global lo excluye automáticamente
	/// </remarks>
	Task<bool> ClientExistsAsync(int clientId);
}
