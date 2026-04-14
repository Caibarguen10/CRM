using CrmService.DTOs;
using CrmService.Common;

namespace CrmService.Services;

/// <summary>
/// Interfaz del servicio de clientes que define la lógica de negocio para la gestión de clientes.
/// </summary>
/// <remarks>
/// CAPA DE SERVICIOS - RESPONSABILIDADES:
/// ------------------------------------------------------------------------
/// Esta capa contiene la LÓGICA DE NEGOCIO de la aplicación. Sus responsabilidades incluyen:
/// 
/// 1. VALIDACIONES DE NEGOCIO:
///    - Verificar que no existan clientes duplicados por número de documento
///    - Validar reglas de negocio complejas que no pueden estar en DTOs
///    - Asegurar consistencia de datos antes de persistir
/// 
/// 2. ORQUESTACIÓN:
///    - Coordinar llamadas a múltiples repositorios si es necesario
///    - Gestionar transacciones complejas
///    - Implementar patrones como Unit of Work si se requiere
/// 
/// 3. MAPEO DE DATOS:
///    - Convertir DTOs (Data Transfer Objects) a Entidades de Dominio
///    - Convertir Entidades de Dominio a DTOs para respuestas
///    - Usar AutoMapper para mapeos automáticos
/// 
/// 4. LOGGING Y OBSERVABILIDAD:
///    - Registrar operaciones importantes para auditoría
///    - Capturar métricas de negocio
///    - Facilitar debugging y troubleshooting
/// 
/// 5. ABSTRACCIÓN:
///    - Ocultar detalles de persistencia (repositorios) a los controladores
///    - Proporcionar una API limpia y orientada a casos de uso
/// 
/// FLUJO DE UNA OPERACIÓN:
/// ------------------------------------------------------------------------
/// Cliente HTTP → Controller → Service (AQUÍ) → Repository → DbContext → SQLite
///                                ↓
///                          Validaciones
///                          Mapeo DTO↔Entity
///                          Logging
///                          Lógica de negocio
/// 
/// ARQUITECTURA DE CAPAS:
/// ------------------------------------------------------------------------
/// 1. Presentation Layer (Controllers): Maneja HTTP, autenticación, autorización
/// 2. Business Logic Layer (Services): Lógica de negocio, validaciones (ESTA CAPA)
/// 3. Data Access Layer (Repositories): Persistencia, queries, EF Core
/// 4. Domain Layer (Entities): Modelos de dominio, reglas de entidades
/// 
/// VENTAJAS DE ESTA ARQUITECTURA:
/// ------------------------------------------------------------------------
/// - TESTABILIDAD: Se pueden hacer mocks de IClientService en tests de controllers
/// - REUTILIZACIÓN: Múltiples controllers pueden usar el mismo servicio
/// - MANTENIBILIDAD: Lógica de negocio centralizada en un solo lugar
/// - SEPARACIÓN: Cada capa tiene una responsabilidad única y clara
/// 
/// PERMISOS POR ROL (validados en Controllers, NO aquí):
/// ------------------------------------------------------------------------
/// - Admin: CRUD completo en todos los clientes
/// - Asesor: Read (consulta) de clientes
/// - Auditor: Solo Read (lectura) de clientes
/// </remarks>
public interface IClientService
{
	/// <summary>
	/// Obtiene una lista paginada de clientes con filtros opcionales.
	/// </summary>
	/// <param name="pagination">Parámetros de paginación (página, tamaño)</param>
	/// <param name="filters">Filtros opcionales (nombre, email, documento)</param>
	/// <returns>Resultado paginado con lista de clientes y metadata de paginación</returns>
	/// <remarks>
	/// CARACTERÍSTICAS:
	/// - Soporta paginación para manejar grandes volúmenes de datos
	/// - Filtros opcionales por nombre, email o documento
	/// - Retorna metadata: total de registros, página actual, tamaño de página
	/// - Respeta soft delete: NO retorna clientes eliminados (IsDeleted = true)
	/// 
	/// EJEMPLO DE USO:
	/// var pagination = new PaginationParams { Page = 1, PageSize = 10 };
	/// var filters = new ClientFilterDto { SearchTerm = "Acme Corp" };
	/// var result = await _clientService.GetAllAsync(pagination, filters);
	/// // result.Items: Lista de 10 clientes máximo
	/// // result.TotalCount: Total de clientes que cumplen el filtro
	/// // result.Page: 1
	/// // result.PageSize: 10
	/// 
	/// LOGGING:
	/// - Registra cada consulta con los parámetros de paginación
	/// - Útil para auditoría y análisis de uso
	/// </remarks>
	Task<PagedResult<ClientDto>> GetAllAsync(PaginationParams pagination, ClientFilterDto? filters = null);

	/// <summary>
	/// Obtiene un cliente específico por su ID.
	/// </summary>
	/// <param name="id">ID del cliente a buscar</param>
	/// <returns>DTO del cliente si existe, null si no se encuentra</returns>
	/// <remarks>
	/// COMPORTAMIENTO:
	/// - Retorna null si el cliente no existe
	/// - Retorna null si el cliente está eliminado (IsDeleted = true)
	/// - Convierte la entidad de dominio a DTO antes de retornar
	/// 
	/// MANEJO EN CONTROLLER:
	/// if (result == null)
	///     return NotFound(new ApiResponse { Success = false, Message = "Cliente no encontrado" });
	/// 
	/// EJEMPLO DE USO:
	/// var client = await _clientService.GetByIdAsync(10);
	/// if (client == null)
	///     throw new NotFoundException("Cliente no encontrado");
	/// 
	/// LOGGING:
	/// - Registra la búsqueda con el ID
	/// - Registra un warning si no se encuentra el cliente
	/// </remarks>
	Task<ClientDto?> GetByIdAsync(int id);

	/// <summary>
	/// Crea un nuevo cliente en el sistema.
	/// </summary>
	/// <param name="client">DTO con los datos del cliente a crear</param>
	/// <returns>DTO del cliente creado con su ID asignado</returns>
	/// <exception cref="InvalidOperationException">Si ya existe un cliente con ese número de documento</exception>
	/// <remarks>
	/// VALIDACIONES DE NEGOCIO:
	/// - Verifica que NO exista otro cliente con el mismo DocumentNumber
	/// - Esta validación es crítica para evitar duplicados
	/// 
	/// PROCESO:
	/// 1. Validar que el DocumentNumber no esté duplicado
	/// 2. Convertir CreateClientDto → Client (entidad de dominio)
	/// 3. Llamar al repositorio para persistir
	/// 4. Convertir Client → ClientDto (respuesta)
	/// 5. Retornar el DTO con ID asignado y auditoría
	/// 
	/// AUDITORÍA AUTOMÁTICA:
	/// El cliente creado tendrá:
	/// - CreatedBy: Usuario desde JWT token
	/// - CreatedAt: Timestamp UTC actual
	/// - IsDeleted: false
	/// 
	/// EJEMPLO DE USO:
	/// var dto = new CreateClientDto 
	/// { 
	///     Name = "Acme Corporation",
	///     DocumentNumber = "12345678",
	///     Email = "contact@acme.com"
	/// };
	/// var created = await _clientService.CreateAsync(dto);
	/// // created.Id = 42 (asignado por BD)
	/// // created.CreatedBy = "admin@crm.com"
	/// 
	/// EXCEPCIÓN:
	/// throw new InvalidOperationException("El cliente ya existe.");
	/// // Se lanza si DocumentNumber ya existe en la BD
	/// 
	/// LOGGING:
	/// - Registra intento de creación con DocumentNumber
	/// - Registra warning si hay duplicado
	/// - Registra éxito con ID asignado
	/// </remarks>
	Task<ClientDto> CreateAsync(CreateClientDto client);

	/// <summary>
	/// Actualiza un cliente existente.
	/// </summary>
	/// <param name="id">ID del cliente a actualizar</param>
	/// <param name="client">DTO con los nuevos datos del cliente</param>
	/// <returns>DTO del cliente actualizado, null si no se encuentra</returns>
	/// <exception cref="InvalidOperationException">Si el nuevo DocumentNumber ya está en uso por otro cliente</exception>
	/// <remarks>
	/// VALIDACIONES DE NEGOCIO:
	/// 1. Verificar que el cliente con ese ID existe
	/// 2. Verificar que el nuevo DocumentNumber no esté usado por OTRO cliente
	///    (se permite que sea el mismo documento si es el mismo cliente)
	/// 
	/// PROCESO:
	/// 1. Buscar el cliente existente por ID
	/// 2. Retornar null si no existe
	/// 3. Validar que el nuevo DocumentNumber no esté duplicado
	/// 4. Mapear los campos del DTO a la entidad existente
	/// 5. Llamar al repositorio para actualizar
	/// 6. Retornar el DTO actualizado
	/// 
	/// AUDITORÍA AUTOMÁTICA:
	/// El cliente actualizado tendrá:
	/// - UpdatedBy: Usuario desde JWT token
	/// - UpdatedAt: Timestamp UTC actual
	/// - CreatedBy/CreatedAt: Se mantienen sin cambios
	/// 
	/// EJEMPLO DE USO:
	/// var dto = new CreateClientDto 
	/// { 
	///     Name = "Acme Corp (actualizado)",
	///     DocumentNumber = "12345678",
	///     Email = "new@acme.com"
	/// };
	/// var updated = await _clientService.UpdateAsync(42, dto);
	/// if (updated == null)
	///     return NotFound();
	/// 
	/// EXCEPCIÓN:
	/// throw new InvalidOperationException("El número de documento ya está en uso por otro cliente.");
	/// // Se lanza si otro cliente (diferente ID) tiene ese DocumentNumber
	/// 
	/// LOGGING:
	/// - Registra intento de actualización con ID
	/// - Registra warning si cliente no existe
	/// - Registra warning si documento está duplicado
	/// - Registra éxito de actualización
	/// </remarks>
	Task<ClientDto?> UpdateAsync(int id, CreateClientDto client);

	/// <summary>
	/// Elimina lógicamente un cliente (soft delete).
	/// </summary>
	/// <param name="id">ID del cliente a eliminar</param>
	/// <returns>true si se eliminó exitosamente, false si no se encontró el cliente</returns>
	/// <remarks>
	/// SOFT DELETE:
	/// - NO se borra físicamente el registro de la base de datos
	/// - Se marca IsDeleted = true en la entidad
	/// - Los filtros globales previenen que aparezca en queries posteriores
	/// 
	/// AUDITORÍA AUTOMÁTICA:
	/// El cliente eliminado tendrá:
	/// - DeletedBy: Usuario desde JWT token
	/// - DeletedAt: Timestamp UTC actual
	/// - IsDeleted: true
	/// 
	/// VENTAJAS DEL SOFT DELETE:
	/// - Permite auditorías posteriores
	/// - Se puede "recuperar" el cliente si fue error
	/// - Mantiene integridad referencial (contactos, notas, oportunidades)
	/// - Cumplimiento normativo (algunas regulaciones prohíben borrado físico)
	/// 
	/// COMPORTAMIENTO:
	/// - Retorna false si el cliente no existe
	/// - Retorna false si el cliente ya estaba eliminado
	/// - Retorna true si se eliminó exitosamente
	/// 
	/// EJEMPLO DE USO:
	/// var deleted = await _clientService.DeleteAsync(42);
	/// if (!deleted)
	///     return NotFound(new ApiResponse { Success = false, Message = "Cliente no encontrado" });
	/// 
	/// LOGGING:
	/// - Registra intento de eliminación con ID
	/// - Registra warning si cliente no existe
	/// - Registra éxito de eliminación
	/// 
	/// NOTA: Los registros relacionados (contactos, notas, oportunidades) NO se eliminan
	/// en cascada automáticamente. Esto es por diseño: se mantiene el historial completo.
	/// </remarks>
	Task<bool> DeleteAsync(int id);
}
