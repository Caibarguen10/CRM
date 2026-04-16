namespace CrmService.Domain;

/// <summary>
/// Entidad que representa una nota o registro de interacción con un cliente.
/// Hereda de BaseEntity para obtener auditoría y soft delete automáticos.
/// </summary>
/// <remarks>
/// Las notas son el historial de comunicaciones e interacciones con el cliente.
/// Pueden incluir llamadas telefónicas, reuniones, emails importantes, acuerdos, etc.
/// El campo CreatedBy de BaseEntity permite saber qué usuario registró cada nota.
/// </remarks>
public class ClientNote : BaseEntity
{
	/// <summary>
	/// Identificador del cliente al que pertenece esta nota.
	/// Clave foránea hacia la tabla Clients.
	/// </summary>
	public int ClientId { get; set; }
	
	/// <summary>
	/// Contenido de la nota o registro de la interacción.
	/// Puede incluir detalles de llamadas, reuniones, acuerdos alcanzados, etc.
	/// Máximo 1000 caracteres.
	/// </summary>
	/// <example>
	/// "Llamada telefónica: Cliente interesado en producto X. 
	/// Acordamos reunión para el 15/05/2024."
	/// </example>
	public string Note { get; set; } = null!;
	
	#region Relaciones de Navegación
	
	/// <summary>
	/// Cliente al que pertenece esta nota.
	/// Relación muchos a uno (varios notas pueden pertenecer a un cliente).
	/// </summary>
	public Client Client { get; set; } = null!;
	
	#endregion
}
