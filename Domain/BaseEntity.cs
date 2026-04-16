namespace CrmService.Domain;

/// <summary>
/// Entidad base abstracta que proporciona funcionalidad común para todas las entidades del dominio.
/// Incluye campos de auditoría (creación, actualización) y soft delete (borrado lógico).
/// </summary>
/// <remarks>
/// Todas las entidades del dominio deben heredar de esta clase para obtener:
/// - Identificador único (Id)
/// - Trazabilidad de cambios (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
/// - Borrado lógico en lugar de físico (IsDeleted, DeletedAt, DeletedBy)
/// </remarks>
public abstract class BaseEntity
{
	/// <summary>
	/// Identificador único de la entidad.
	/// Se genera automáticamente por la base de datos.
	/// </summary>
	public int Id { get; set; }
	
	#region Auditoría
	
	/// <summary>
	/// Fecha y hora UTC de creación del registro.
	/// Se asigna automáticamente en SaveChanges() del DbContext.
	/// </summary>
	public DateTime CreatedAt { get; set; }
	
	/// <summary>
	/// Usuario que creó el registro.
	/// Se obtiene del JWT token en el momento de la creación.
	/// Valor por defecto: "System" si no hay usuario autenticado.
	/// </summary>
	public string CreatedBy { get; set; } = string.Empty;
	
	/// <summary>
	/// Fecha y hora UTC de la última actualización del registro.
	/// Se asigna automáticamente cuando se modifica la entidad.
	/// Null si nunca se ha actualizado.
	/// </summary>
	public DateTime? UpdatedAt { get; set; }
	
	/// <summary>
	/// Usuario que realizó la última actualización.
	/// Se obtiene del JWT token en el momento de la actualización.
	/// Null si nunca se ha actualizado.
	/// </summary>
	public string? UpdatedBy { get; set; }
	
	#endregion
	
	#region Soft Delete (Borrado Lógico)
	
	/// <summary>
	/// Indica si el registro ha sido eliminado lógicamente.
	/// Los registros con IsDeleted = true no se muestran en consultas normales
	/// gracias al filtro global configurado en el DbContext.
	/// </summary>
	public bool IsDeleted { get; set; }
	
	/// <summary>
	/// Fecha y hora UTC en que se eliminó el registro.
	/// Se asigna automáticamente cuando se marca IsDeleted = true.
	/// Null si el registro no ha sido eliminado.
	/// </summary>
	public DateTime? DeletedAt { get; set; }
	
	/// <summary>
	/// Usuario que eliminó el registro.
	/// Se obtiene del JWT token en el momento de la eliminación.
	/// Null si el registro no ha sido eliminado.
	/// </summary>
	public string? DeletedBy { get; set; }
	
	#endregion
}
