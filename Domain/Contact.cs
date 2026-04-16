namespace CrmService.Domain;

/// <summary>
/// Entidad que representa una persona de contacto asociada a un cliente.
/// Hereda de BaseEntity para obtener auditoría y soft delete automáticos.
/// </summary>
/// <remarks>
/// Un contacto es una persona específica dentro de una organización cliente
/// con quien se mantiene comunicación (ej: gerente, contador, asistente).
/// Cada contacto está vinculado a un único cliente.
/// </remarks>
public class Contact : BaseEntity
{
	/// <summary>
	/// Identificador del cliente al que pertenece este contacto.
	/// Clave foránea hacia la tabla Clients.
	/// </summary>
	public int ClientId { get; set; }
	
	/// <summary>
	/// Nombre completo de la persona de contacto.
	/// Máximo 150 caracteres.
	/// </summary>
	public string Name { get; set; } = null!;
	
	/// <summary>
	/// Cargo o posición de la persona dentro de la organización.
	/// Ejemplo: "Gerente General", "Contador", "Asistente Administrativa".
	/// Máximo 100 caracteres.
	/// </summary>
	public string Position { get; set; } = null!;
	
	/// <summary>
	/// Dirección de correo electrónico del contacto.
	/// Máximo 150 caracteres.
	/// </summary>
	public string Email { get; set; } = null!;
	
	/// <summary>
	/// Número de teléfono del contacto.
	/// Puede ser celular, extensión, o teléfono directo.
	/// Máximo 30 caracteres.
	/// </summary>
	public string Phone { get; set; } = null!;
	
	#region Relaciones de Navegación
	
	/// <summary>
	/// Cliente al que pertenece este contacto.
	/// Relación muchos a uno (varios contactos pueden pertenecer a un cliente).
	/// </summary>
	public Client Client { get; set; } = null!;
	
	#endregion
}
