namespace CrmService.Domain;

/// <summary>
/// Entidad que representa un cliente en el sistema CRM.
/// Hereda de BaseEntity para obtener auditoría y soft delete automáticos.
/// </summary>
/// <remarks>
/// Un cliente es la entidad principal del CRM y puede tener:
/// - Múltiples contactos (personas de contacto)
/// - Múltiples notas (historial de interacciones)
/// - Múltiples oportunidades de negocio
/// </remarks>
public class Client : BaseEntity
{
	/// <summary>
	/// Número de documento de identidad del cliente (DNI, RUC, etc.).
	/// Es único en el sistema y no puede repetirse.
	/// Máximo 50 caracteres.
	/// </summary>
	public string DocumentNumber { get; set; } = null!;
	
	/// <summary>
	/// Nombre completo o razón social del cliente.
	/// Máximo 200 caracteres.
	/// </summary>
	public string FullName { get; set; } = null!;
	
	/// <summary>
	/// Dirección de correo electrónico del cliente.
	/// Máximo 150 caracteres.
	/// </summary>
	public string Email { get; set; } = null!;
	
	/// <summary>
	/// Número de teléfono del cliente.
	/// Máximo 30 caracteres.
	/// </summary>
	public string Phone { get; set; } = null!;
	
	#region Relaciones de Navegación
	
	/// <summary>
	/// Colección de contactos asociados a este cliente.
	/// Relación uno a muchos (un cliente puede tener varios contactos).
	/// </summary>
	public ICollection<Contact> Contacts { get; set; } = new List<Contact>();
	
	/// <summary>
	/// Colección de notas asociadas a este cliente.
	/// Relación uno a muchos (un cliente puede tener varias notas).
	/// Las notas registran el historial de interacciones con el cliente.
	/// </summary>
	public ICollection<ClientNote> Notes { get; set; } = new List<ClientNote>();
	
	/// <summary>
	/// Colección de oportunidades de negocio asociadas a este cliente.
	/// Relación uno a muchos (un cliente puede tener varias oportunidades).
	/// </summary>
	public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
	
	#endregion
}
