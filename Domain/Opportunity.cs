namespace CrmService.Domain;

/// <summary>
/// Entidad que representa una oportunidad de negocio asociada a un cliente.
/// Hereda de BaseEntity para obtener auditoría y soft delete automáticos.
/// </summary>
/// <remarks>
/// Una oportunidad representa una posible venta o negocio en proceso.
/// Permite hacer seguimiento del pipeline de ventas y estimar ingresos futuros.
/// El estado permite conocer en qué fase del proceso se encuentra la oportunidad.
/// </remarks>
public class Opportunity : BaseEntity
{
	/// <summary>
	/// Identificador del cliente al que pertenece esta oportunidad.
	/// Clave foránea hacia la tabla Clients.
	/// </summary>
	public int ClientId { get; set; }
	
	/// <summary>
	/// Título o nombre descriptivo de la oportunidad de negocio.
	/// Ejemplo: "Venta de Software ERP", "Proyecto de Consultoría 2024".
	/// Máximo 200 caracteres.
	/// </summary>
	public string Title { get; set; } = null!;
	
	/// <summary>
	/// Monto estimado de la oportunidad en la moneda del sistema.
	/// Representa el valor potencial del negocio si se cierra exitosamente.
	/// Precisión: 18 dígitos totales, 2 decimales.
	/// </summary>
	/// <example>
	/// 15000.50 (representa $15,000.50 o S/ 15,000.50 según la moneda)
	/// </example>
	public decimal EstimatedAmount { get; set; }
	
	/// <summary>
	/// Estado actual de la oportunidad en el proceso de ventas.
	/// Ejemplos comunes: "Prospecto", "Calificada", "Propuesta", "Negociación", 
	/// "Ganada", "Perdida", "En Espera".
	/// Máximo 50 caracteres.
	/// </summary>
	/// <remarks>
	/// Se recomienda usar valores estandarizados para facilitar reportes.
	/// En una implementación futura se podría convertir en un enum.
	/// </remarks>
	public string Status { get; set; } = null!;
	
	#region Relaciones de Navegación
	
	/// <summary>
	/// Cliente al que pertenece esta oportunidad.
	/// Relación muchos a uno (varias oportunidades pueden pertenecer a un cliente).
	/// </summary>
	public Client Client { get; set; } = null!;
	
	#endregion
}
