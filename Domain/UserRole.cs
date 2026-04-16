namespace CrmService.Domain;

/// <summary>
/// Enumeración que define los roles de usuario disponibles en el sistema CRM.
/// Estos roles se utilizan para controlar el acceso a diferentes funcionalidades.
/// </summary>
/// <remarks>
/// Matriz de permisos por rol:
/// 
/// - Admin (1): Control total del sistema
///   * CRUD completo en Clientes, Contactos, Notas y Oportunidades
///   * Gestión de usuarios
///   * Acceso a todas las funcionalidades
/// 
/// - Asesor (2): Usuario operativo principal
///   * Consulta (Read) de Clientes, Contactos y Oportunidades
///   * CRUD completo en Notas (su actividad principal)
///   * No puede modificar clientes ni oportunidades
/// 
/// - Auditor (3): Usuario de solo lectura
///   * Solo consulta (Read) en todas las entidades
///   * Sin permisos de escritura (Create, Update, Delete)
///   * Usado para supervisión y reportes
/// </remarks>
public enum UserRole
{
	/// <summary>
	/// Administrador del sistema con permisos completos.
	/// Puede realizar todas las operaciones CRUD en todas las entidades.
	/// </summary>
	Admin = 1,
	
	/// <summary>
	/// Asesor comercial con permisos limitados.
	/// Puede consultar información y gestionar notas de clientes.
	/// </summary>
	Asesor = 2,
	
	/// <summary>
	/// Auditor con permisos de solo lectura.
	/// Puede ver toda la información pero no puede modificarla.
	/// </summary>
	Auditor = 3
}
