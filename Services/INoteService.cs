using CrmService.DTOs;

namespace CrmService.Services;

/// <summary>
/// Interfaz del servicio de notas que define la lógica de negocio para gestión de notas de clientes.
/// </summary>
/// <remarks>
/// PROPÓSITO DE LAS NOTAS:
/// Registrar interacciones, comentarios y seguimiento de comunicaciones con clientes.
/// 
/// CASOS DE USO:
/// - Documentar llamadas telefónicas
/// - Registrar reuniones presenciales
/// - Seguimiento de emails importantes
/// - Recordatorios y tareas pendientes
/// - Historial de comunicación completo
/// 
/// CARACTERÍSTICAS:
/// - Cada nota PERTENECE a un cliente (relación 1:N)
/// - INMUTABLES: No se pueden editar después de creadas (diseño intencional)
/// - Auditoría automática (CreatedBy, CreatedAt)
/// - Soft delete disponible
/// 
/// PERMISOS POR ROL:
/// - Admin: CRUD completo en notas
/// - Asesor: CRUD completo (su función principal es documentar interacciones)
/// - Auditor: Solo Read (puede ver historial pero NO modificar)
/// </remarks>
public interface INoteService
{
	/// <summary>
	/// Crea una nueva nota asociada a un cliente.
	/// </summary>
	/// <param name="dto">DTO con el contenido de la nota</param>
	/// <returns>ID de la nota creada</returns>
	/// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
	/// <remarks>
	/// VALIDACIONES:
	/// - Cliente debe existir en base de datos
	/// - Content NO puede estar vacío (validado en DTO)
	/// - Content máximo 5000 caracteres
	/// 
	/// AUDITORÍA AUTOMÁTICA:
	/// - CreatedBy: Usuario del JWT token (quien escribe la nota)
	/// - CreatedAt: Timestamp UTC actual (cuándo se escribió)
	/// 
	/// INMUTABILIDAD:
	/// Las notas NO se pueden editar después de creadas porque son
	/// registros históricos de comunicación. Si hay un error, la
	/// solución es crear una nueva nota con la corrección.
	/// 
	/// EJEMPLO:
	/// var dto = new CreateNoteDto 
	/// { 
	///     Content = "Llamada recibida. Cliente interesado en plan Premium.",
	///     ClientId = 15
	/// };
	/// var noteId = await _noteService.CreateAsync(dto);
	/// // noteId = 128
	/// </remarks>
	Task<int> CreateAsync(CreateNoteDto dto);
}
