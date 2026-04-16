using CrmService.DTOs;

namespace CrmService.Services;

/// <summary>
/// Interfaz del servicio de contactos que define la lógica de negocio para gestión de contactos de clientes.
/// </summary>
/// <remarks>
/// PROPÓSITO:
/// Gestionar personas de contacto asociadas a clientes (Gerentes, Asistentes, etc.)
/// 
/// CARACTERÍSTICAS:
/// - Cada contacto PERTENECE a un cliente (relación 1:N)
/// - Validación de integridad referencial (cliente debe existir)
/// - Auditoría automática (CreatedBy, CreatedAt)
/// - Soft delete heredado de BaseEntity
/// 
/// PERMISOS POR ROL:
/// - Admin: CRUD completo en contactos
/// - Asesor: Read contactos + CRUD en notas asociadas
/// - Auditor: Solo Read (lectura)
/// </remarks>
public interface IContactService
{
	/// <summary>
	/// Obtiene todos los contactos de un cliente específico.
	/// </summary>
	/// <param name="clientId">ID del cliente</param>
	/// <returns>Lista de contactos del cliente ordenados alfabéticamente</returns>
	/// <remarks>
	/// - Retorna lista vacía si el cliente no tiene contactos
	/// - Respeta soft delete: NO incluye contactos eliminados
	/// - Orden alfabético por nombre (A-Z)
	/// </remarks>
	Task<List<ContactDto>> GetByClientIdAsync(int clientId);

	/// <summary>
	/// Crea un nuevo contacto para un cliente.
	/// </summary>
	/// <param name="dto">DTO con datos del contacto</param>
	/// <returns>DTO del contacto creado con ID asignado</returns>
	/// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
	/// <remarks>
	/// VALIDACIONES:
	/// - Cliente debe existir en base de datos
	/// - Email y Name son obligatorios (validado en DTO)
	/// 
	/// AUDITORÍA AUTOMÁTICA:
	/// - CreatedBy: Usuario del JWT token
	/// - CreatedAt: Timestamp UTC actual
	/// </remarks>
	Task<ContactDto> CreateAsync(CreateContactDto dto);
}
