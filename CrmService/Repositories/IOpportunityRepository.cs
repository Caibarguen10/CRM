using CrmService.Domain;

namespace CrmService.Repositories;

/// <summary>
/// Interfaz del repositorio de oportunidades de negocio que define las operaciones
/// de acceso a datos para la entidad Opportunity.
/// </summary>
/// <remarks>
/// QUÉ ES UNA OPORTUNIDAD EN CRM:
/// ------------------------------------
/// Una "oportunidad" representa una posible venta o negocio en desarrollo con un cliente.
/// Es el núcleo del proceso comercial en un CRM. Ejemplos:
/// 
/// - Venta potencial de software por $50,000 USD
/// - Renovación de contrato anual con cliente existente
/// - Upgrade de plan básico a plan empresarial
/// - Proyecto de consultoría estimado en $25,000 USD
/// - Expansión de servicios a nuevas sucursales del cliente
/// 
/// CICLO DE VIDA DE UNA OPORTUNIDAD:
/// ------------------------------------
/// 1. Lead (Prospecto) → Cliente muestra interés inicial
/// 2. Qualified (Calificado) → Se valida que tiene presupuesto y necesidad real
/// 3. Proposal (Propuesta) → Se envía cotización o propuesta formal
/// 4. Negotiation (Negociación) → Se discuten términos, precios, condiciones
/// 5. Closed Won (Ganada) → ¡Éxito! El cliente firmó/compró
/// 6. Closed Lost (Perdida) → El cliente decidió no comprar o eligió competencia
/// 
/// PROPÓSITO DEL PATRÓN REPOSITORY:
/// ------------------------------------
/// Esta interfaz abstrae la lógica de acceso a datos de Entity Framework Core,
/// proporcionando múltiples beneficios:
/// 
/// 1. SEPARACIÓN DE RESPONSABILIDADES:
///    - OpportunityService NO conoce detalles de EF Core, LINQ, ni SQL
///    - Los controladores NO acceden directamente a AppDbContext
///    - Facilita reemplazar EF Core por otro ORM sin afectar lógica de negocio
/// 
/// 2. TESTABILIDAD:
///    - Se pueden crear mocks de IOpportunityRepository en tests unitarios
///    - No se requiere base de datos real para probar OpportunityService
///    - Ejemplo: Mock que retorna ClientExistsAsync() = true siempre
/// 
/// 3. REUTILIZACIÓN:
///    - La misma validación ClientExistsAsync() se usa desde múltiples servicios
///    - Evita duplicación de código de acceso a datos
///    - Centraliza queries complejas en un solo lugar
/// 
/// CARACTERÍSTICAS DE LAS OPORTUNIDADES:
/// ------------------------------------
/// - Relación 1:N con Cliente (una oportunidad PERTENECE a un cliente)
/// - Monto en formato decimal(18,2) para precisión monetaria
/// - Enum Status con valores predefinidos (Lead, Qualified, Proposal, etc.)
/// - Auditoría automática heredada de BaseEntity (CreatedBy, CreatedAt, etc.)
/// - Soft delete: oportunidades eliminadas NO se borran físicamente
/// - Inmutables después de creación: NO se pueden editar (solo crear)
/// 
/// PERMISOS POR ROL (Sistema de Autorización):
/// ------------------------------------
/// - Admin: CRUD completo en oportunidades de cualquier cliente
/// - Asesor: Read en oportunidades + crear nuevas (pero NO editar/eliminar)
/// - Auditor: Solo Read (lectura) - puede ver pipeline de ventas
/// 
/// NOTA: La verificación de permisos se hace en OpportunitiesController/OpportunityService,
/// NO en el repositorio. El repositorio solo ejecuta operaciones de datos.
/// 
/// MÉTRICAS CLAVE EN CRM (calculadas desde oportunidades):
/// ------------------------------------
/// - Pipeline total: Suma de montos de oportunidades activas (Status != Closed)
/// - Tasa de conversión: % de oportunidades Closed Won vs Total
/// - Ciclo de venta promedio: Tiempo promedio desde Lead hasta Closed Won
/// - Valor promedio de venta: Promedio de Amount en oportunidades Closed Won
/// </remarks>
public interface IOpportunityRepository
{
	/// <summary>
	/// Crea una nueva oportunidad de negocio asociada a un cliente.
	/// </summary>
	/// <param name="opportunity">Entidad oportunidad con los datos a guardar</param>
	/// <returns>La oportunidad creada con su ID asignado y campos de auditoría populados</returns>
	/// <remarks>
	/// PROCESO AUTOMÁTICO AL GUARDAR (AppDbContext.SaveChangesAsync):
	/// ------------------------------------------------------------------------
	/// 1. EF Core detecta el estado "Added" de la oportunidad
	/// 2. El método ProcessAuditFields() en AppDbContext intercepta la operación
	/// 3. Asigna automáticamente:
	///    - CreatedBy = nombre del usuario actual obtenido del JWT token
	///    - CreatedAt = DateTime.UtcNow (hora actual en formato UTC)
	///    - IsDeleted = false (valor por defecto)
	/// 4. EF Core genera el INSERT SQL con parámetros
	/// 5. SQLite ejecuta el INSERT y retorna el ID autogenerado
	/// 6. EF Core actualiza opportunity.Id con el valor retornado
	/// 7. Retorna la oportunidad con ID asignado y auditoría completa
	/// 
	/// SQL GENERADO APROXIMADO:
	/// INSERT INTO Opportunities (Title, Description, Amount, Status, ClientId, 
	///                            CreatedBy, CreatedAt, IsDeleted)
	/// VALUES (@title, @description, @amount, @status, @clientId,
	///         @createdBy, @createdAt, 0);
	/// SELECT last_insert_rowid(); -- Retorna el nuevo ID
	/// 
	/// VALIDACIONES REQUERIDAS (en OpportunityService, NO aquí):
	/// - El ClientId debe existir en la tabla Clients (usar ClientExistsAsync)
	/// - Title NO debe estar vacío (obligatorio)
	/// - Amount debe ser >= 0 (no se permiten montos negativos)
	/// - Amount tiene precisión decimal(18,2) para valores monetarios exactos
	/// - Status debe ser un valor válido del enum (Lead, Qualified, etc.)
	/// 
	/// EJEMPLO DE USO:
	/// var newOpportunity = new Opportunity 
	/// { 
	///     Title = "Venta Plan Enterprise",
	///     Description = "Cliente interesado en migrar 200 usuarios a plan empresarial",
	///     Amount = 75000.00m,
	///     Status = OpportunityStatus.Proposal,
	///     ClientId = 42
	/// };
	/// var saved = await _opportunityRepo.CreateAsync(newOpportunity);
	/// // saved.Id = 89 (ID asignado por SQLite)
	/// // saved.CreatedBy = "asesor@crm.com" (extraído del JWT token)
	/// // saved.CreatedAt = 2026-04-14 17:30:00 UTC
	/// 
	/// IMPORTANCIA DEL CAMPO AMOUNT:
	/// ------------------------------------------------------------------------
	/// - Tipo: decimal(18,2) → Precisión exacta para cálculos monetarios
	/// - NO usar float/double → Tienen errores de redondeo ($0.10 puede ser $0.099999)
	/// - Ejemplos de uso:
	///   * Amount = 15000.00m → Oportunidad de $15,000.00 USD
	///   * Amount = 125500.50m → Oportunidad de $125,500.50 USD
	/// - Se usa para calcular pipeline total: SUM(Amount) WHERE Status != 'Closed Lost'
	/// 
	/// TRAZABILIDAD Y AUDITORÍA:
	/// ------------------------------------------------------------------------
	/// Cada oportunidad queda registrada con:
	/// - Quién la creó (CreatedBy) → Usuario desde JWT (ej. "vendedor@crm.com")
	/// - Cuándo la creó (CreatedAt) → Timestamp UTC preciso
	/// - A qué cliente pertenece (ClientId) → Foreign Key
	/// - Estado actual (Status) → Lead, Qualified, Proposal, etc.
	/// 
	/// Esto permite:
	/// - Reportes de desempeño por vendedor
	/// - Análisis de pipeline histórico
	/// - Auditorías de compliance
	/// - Forecasting de ventas
	/// </remarks>
	Task<Opportunity> CreateAsync(Opportunity opportunity);

	/// <summary>
	/// Verifica si existe un cliente con el ID especificado (sin estar eliminado).
	/// </summary>
	/// <param name="clientId">ID del cliente a verificar</param>
	/// <returns>true si el cliente existe y NO está eliminado, false en caso contrario</returns>
	/// <remarks>
	/// PROPÓSITO:
	/// - Validar integridad referencial ANTES de crear una oportunidad
	/// - Evitar crear oportunidades huérfanas (asociadas a clientes inexistentes o eliminados)
	/// - Proporcionar mensajes de error descriptivos al usuario
	/// 
	/// COMPORTAMIENTO DEL FILTRO GLOBAL:
	/// - Respeta el filtro global de soft delete configurado en AppDbContext
	/// - Si el cliente fue eliminado lógicamente (IsDeleted = true), retorna false
	/// - Esto previene crear oportunidades para clientes que ya no están activos
	/// 
	/// OPTIMIZACIÓN:
	/// - Usa AnyAsync() en lugar de CountAsync() o FirstOrDefaultAsync()
	/// - AnyAsync() es más eficiente: se detiene apenas encuentra 1 registro
	/// - No carga datos innecesarios en memoria, solo verifica existencia
	/// 
	/// SQL GENERADO APROXIMADO:
	/// SELECT CASE WHEN EXISTS(
	///     SELECT 1 FROM Clients WHERE Id = @clientId AND IsDeleted = 0
	/// ) THEN 1 ELSE 0 END
	/// 
	/// FLUJO TÍPICO EN SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityDto dto)
	/// {
	///     // 1. Validar que el cliente existe
	///     if (!await _opportunityRepo.ClientExistsAsync(dto.ClientId))
	///         throw new NotFoundException($"Cliente con ID {dto.ClientId} no encontrado");
	///     
	///     // 2. Validar monto (debe ser positivo)
	///     if (dto.Amount < 0)
	///         throw new ValidationException("El monto no puede ser negativo");
	///     
	///     // 3. Mapear DTO a entidad
	///     var opportunity = _mapper.Map<Opportunity>(dto);
	///     
	///     // 4. Crear en base de datos (auditoría automática)
	///     var created = await _opportunityRepo.CreateAsync(opportunity);
	///     
	///     // 5. Retornar DTO con ID asignado
	///     return _mapper.Map<OpportunityDto>(created);
	/// }
	/// 
	/// VENTAJA SOBRE NO VALIDAR:
	/// ------------------------------------------------------------------------
	/// SIN validación previa:
	/// - El INSERT falla con SqlException: "FOREIGN KEY constraint failed"
	/// - Mensaje técnico y críptico para el usuario final
	/// - Se retorna un error 500 Internal Server Error genérico
	/// - Dificulta debugging y logs
	/// 
	/// CON validación previa usando ClientExistsAsync():
	/// - Se detecta el problema ANTES de intentar el INSERT
	/// - Se lanza NotFoundException con mensaje descriptivo en español
	/// - Se retorna un error 404 Not Found con mensaje claro
	/// - Mejor experiencia de usuario (UX)
	/// - Facilita debugging: el log muestra exactamente qué cliente no existe
	/// 
	/// CASOS DE USO:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Cliente existe y está activo:
	///   bool exists = await ClientExistsAsync(42);
	///   // Retorna: true → Permitir creación de la oportunidad
	/// 
	/// Caso 2 - Cliente NO existe:
	///   bool exists = await ClientExistsAsync(9999);
	///   // Retorna: false → Lanzar NotFoundException
	/// 
	/// Caso 3 - Cliente eliminado (IsDeleted = true):
	///   bool exists = await ClientExistsAsync(10);
	///   // Retorna: false → El filtro global lo excluye automáticamente
	///   // Tiene sentido de negocio: NO crear oportunidades para clientes inactivos
	/// 
	/// REGLAS DE NEGOCIO:
	/// ------------------------------------------------------------------------
	/// - NO se pueden crear oportunidades para clientes soft-deleted
	/// - Esto mantiene consistencia: si un cliente está "inactivo",
	///   no tiene sentido crear nuevas oportunidades de venta con él
	/// - Si se reactiva el cliente (IsDeleted = false), entonces SÍ se pueden
	///   crear nuevas oportunidades
	/// </remarks>
	Task<bool> ClientExistsAsync(int clientId);
}
