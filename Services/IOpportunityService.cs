using CrmService.DTOs;

namespace CrmService.Services;

/// <summary>
/// Interfaz del servicio de oportunidades que define la lógica de negocio para gestión de oportunidades de venta.
/// </summary>
/// <remarks>
/// QUÉ ES UNA OPORTUNIDAD:
/// Una oportunidad representa una posible venta o negocio potencial con un cliente.
/// Es el núcleo del proceso comercial en un CRM.
/// 
/// EJEMPLOS:
/// - Venta de software por $50,000 USD
/// - Renovación de contrato anual
/// - Proyecto de consultoría estimado en $25,000 USD
/// - Upgrade de plan básico a empresarial
/// 
/// ESTADOS DE UNA OPORTUNIDAD (OpportunityStatus):
/// - Lead: Prospecto inicial, cliente mostró interés
/// - Qualified: Cliente calificado, tiene presupuesto y necesidad
/// - Proposal: Propuesta enviada, cotización presentada
/// - Negotiation: Negociando términos y precios
/// - ClosedWon: ¡Ganada! Cliente firmó/compró
/// - ClosedLost: Perdida, cliente no compró
/// 
/// CARACTERÍSTICAS:
/// - Cada oportunidad PERTENECE a un cliente (relación 1:N)
/// - Monto en decimal(18,2) para precisión monetaria exacta
/// - INMUTABLES: No se pueden editar (diseño intencional)
/// - Auditoría automática (CreatedBy, CreatedAt)
/// 
/// MÉTRICAS CALCULADAS:
/// - Pipeline total: Suma de Amount de oportunidades activas
/// - Tasa de conversión: % ClosedWon / Total
/// - Valor promedio de venta: AVG(Amount) WHERE ClosedWon
/// - Ciclo de venta: Tiempo promedio hasta cerrar
/// 
/// PERMISOS POR ROL:
/// - Admin: CRUD completo
/// - Asesor: Read + Create (puede ver y crear, NO editar/eliminar)
/// - Auditor: Solo Read (puede ver pipeline pero NO modificar)
/// </remarks>
public interface IOpportunityService
{
	/// <summary>
	/// Crea una nueva oportunidad de venta para un cliente.
	/// </summary>
	/// <param name="dto">DTO con datos de la oportunidad</param>
	/// <returns>ID de la oportunidad creada</returns>
	/// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
	/// <remarks>
	/// VALIDACIONES:
	/// - Cliente debe existir en base de datos
	/// - Title NO puede estar vacío (obligatorio)
	/// - Amount debe ser >= 0 (no se permiten montos negativos)
	/// - Status debe ser valor válido del enum
	/// 
	/// AUDITORÍA AUTOMÁTICA:
	/// - CreatedBy: Usuario del JWT token (vendedor)
	/// - CreatedAt: Timestamp UTC actual
	/// 
	/// PRECISIÓN MONETARIA:
	/// Amount usa decimal(18,2) para evitar errores de redondeo.
	/// NO usar float/double para valores monetarios.
	/// 
	/// INMUTABILIDAD:
	/// Las oportunidades NO se pueden editar porque son registros
	/// históricos del pipeline. Si cambia algo, se crea una nueva.
	/// 
	/// EJEMPLO:
	/// var dto = new CreateOpportunityDto 
	/// { 
	///     Title = "Venta Plan Enterprise",
	///     Description = "Migración de 200 usuarios",
	///     Amount = 75000.00m,
	///     Status = OpportunityStatus.Proposal,
	///     ClientId = 42
	/// };
	/// var oppId = await _opportunityService.CreateAsync(dto);
	/// // oppId = 312
	/// </remarks>
	Task<int> CreateAsync(CreateOpportunityDto dto);
}
