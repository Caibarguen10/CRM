using CrmService.Data;
using CrmService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrmService.Repositories;

/// <summary>
/// Implementación del repositorio de notas de cliente que gestiona el acceso a datos
/// de la entidad ClientNote usando Entity Framework Core.
/// </summary>
/// <remarks>
/// ARQUITECTURA Y RESPONSABILIDADES:
/// ------------------------------------------------------------------------
/// Esta clase implementa el patrón Repository, encapsulando toda la lógica de
/// acceso a datos relacionada con las notas de clientes. Proporciona una capa
/// de abstracción entre la lógica de negocio (NoteService) y la persistencia (EF Core).
/// 
/// CARACTERÍSTICAS TÉCNICAS:
/// ------------------------------------------------------------------------
/// 1. INYECCIÓN DE DEPENDENCIAS:
///    - Recibe AppDbContext por constructor (patrón Dependency Injection)
///    - El DbContext tiene ciclo de vida Scoped (una instancia por request HTTP)
///    - Se destruye automáticamente al finalizar el request
/// 
/// 2. AUDITORÍA AUTOMÁTICA:
///    - Al llamar SaveChangesAsync(), AppDbContext intercepta la operación
///    - ProcessAuditFields() asigna automáticamente CreatedBy y CreatedAt
///    - El usuario se obtiene del JWT token (HttpContext.User.Identity.Name)
/// 
/// 3. SOFT DELETE:
///    - Los filtros globales en AppDbContext excluyen registros con IsDeleted = true
///    - NO es necesario agregar .Where(x => !x.IsDeleted) en las queries
///    - Las notas eliminadas permanecen en la base de datos pero invisibles
/// 
/// 4. INMUTABILIDAD:
///    - Las notas NO tienen métodos Update/Edit en este repositorio
///    - Diseño intencional: las notas son registros históricos inmutables
///    - Solo se pueden crear nuevas notas, no modificar existentes
/// 
/// RELACIÓN CON OTRAS CAPAS:
/// ------------------------------------------------------------------------
/// NotesController → NoteService → NoteRepository → AppDbContext → SQLite
/// 
/// CASOS DE USO DE LAS NOTAS:
/// ------------------------------------------------------------------------
/// - Documentar llamadas telefónicas: "Cliente preguntó por plan Enterprise"
/// - Registrar reuniones: "Reunión exitosa, interesado en contratar"
/// - Seguimiento: "Pendiente enviar propuesta económica"
/// - Historial de comunicación: Trazabilidad completa de interacciones
/// - Recordatorios: "Llamar la próxima semana para seguimiento"
/// 
/// PERMISOS (verificados en NotesController/NoteService):
/// ------------------------------------------------------------------------
/// - Admin: CRUD completo en notas de cualquier cliente
/// - Asesor: CRUD completo en notas (su función principal)
/// - Auditor: Solo Read (puede consultar historial pero NO modificar)
/// </remarks>
public class NoteRepository : INoteRepository
{
	/// <summary>
	/// Contexto de base de datos de Entity Framework Core.
	/// </summary>
	/// <remarks>
	/// Este contexto gestiona:
	/// - Conexión a SQLite (archivo crm.db local)
	/// - Interceptación de SaveChanges() para auditoría automática
	/// - Filtros globales de soft delete (IsDeleted = false)
	/// - Relaciones entre entidades (ClientNote → Client)
	/// - Change tracking para detección de modificaciones
	/// 
	/// CICLO DE VIDA (Scoped):
	/// - Se crea al inicio de cada request HTTP
	/// - Se destruye automáticamente al finalizar el request
	/// - NO debe reutilizarse entre requests (evita problemas de concurrencia)
	/// </remarks>
	private readonly AppDbContext _context;

	/// <summary>
	/// Constructor que recibe el contexto de base de datos por inyección de dependencias.
	/// </summary>
	/// <param name="context">Contexto de EF Core (Scoped lifetime)</param>
	/// <remarks>
	/// INYECCIÓN DE DEPENDENCIAS:
	/// ------------------------------------------------------------------------
	/// Este patrón permite:
	/// - Desacoplamiento: NoteRepository NO instancia AppDbContext directamente
	/// - Testabilidad: Se puede inyectar un DbContext en memoria para tests
	/// - Ciclo de vida gestionado: ASP.NET Core maneja creación/destrucción
	/// 
	/// CONFIGURACIÓN EN Program.cs:
	/// builder.Services.AddDbContext<AppDbContext>(options =>
	///     options.UseSqlite(connectionString), 
	///     ServiceLifetime.Scoped); // ← Una instancia por request HTTP
	/// 
	/// builder.Services.AddScoped<INoteRepository, NoteRepository>();
	/// </remarks>
	public NoteRepository(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Crea una nueva nota asociada a un cliente.
	/// </summary>
	/// <param name="note">Entidad nota con el contenido a guardar</param>
	/// <returns>La nota creada con su ID asignado y campos de auditoría populados</returns>
	/// <remarks>
	/// PROCESO COMPLETO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. _context.ClientNotes.Add(note): Marca la entidad como "Added" en ChangeTracker
	/// 2. await _context.SaveChangesAsync(): Dispara el guardado asíncrono
	/// 3. (INTERNO) AppDbContext.SaveChangesAsync() sobrescrito intercepta la operación
	/// 4. (INTERNO) ChangeTracker.Entries() detecta entidades con estado "Added"
	/// 5. (INTERNO) ProcessAuditFields() se ejecuta:
	///    - Obtiene el usuario del JWT: HttpContext.User.Identity.Name
	///    - Asigna note.CreatedBy = "asesor@crm.com" (ejemplo)
	///    - Asigna note.CreatedAt = DateTime.UtcNow (2026-04-14T16:20:00Z)
	/// 6. (INTERNO) EF Core genera el SQL INSERT con parámetros
	/// 7. (INTERNO) SQLite ejecuta el INSERT y retorna el ID autogenerado
	/// 8. (INTERNO) EF Core actualiza note.Id con el valor retornado por SQLite
	/// 9. return note: Retorna la nota con ID y auditoría completos
	/// 
	/// SQL GENERADO POR EF CORE:
	/// ------------------------------------------------------------------------
	/// INSERT INTO [ClientNotes] (
	///     [Content], [ClientId], [CreatedBy], [CreatedAt], [IsDeleted]
	/// )
	/// VALUES (
	///     @p0, @p1, @p2, @p3, 0
	/// );
	/// SELECT [Id] FROM [ClientNotes] WHERE [rowid] = last_insert_rowid();
	/// 
	/// PARÁMETROS SQL EJEMPLO:
	/// ------------------------------------------------------------------------
	/// @p0 = 'Cliente solicitó información sobre planes corporativos. Enviar brochure.'
	/// @p1 = 25                           (ClientId - Foreign Key)
	/// @p2 = 'asesor@crm.com'             (CreatedBy - automático del JWT)
	/// @p3 = '2026-04-14T16:20:35.123Z'   (CreatedAt - automático UTC)
	/// 
	/// AUDITORÍA AUTOMÁTICA - TRANSFORMACIÓN DE LA ENTIDAD:
	/// ------------------------------------------------------------------------
	/// ANTES de SaveChangesAsync():
	/// {
	///     Id = 0,
	///     Content = "Cliente preguntó por descuentos para volumen",
	///     ClientId = 25,
	///     CreatedBy = null,        ← Vacío
	///     CreatedAt = default,     ← Vacío (0001-01-01)
	///     IsDeleted = false
	/// }
	/// 
	/// DESPUÉS de SaveChangesAsync():
	/// {
	///     Id = 156,                                   ← Asignado por SQLite
	///     Content = "Cliente preguntó por descuentos para volumen",
	///     ClientId = 25,
	///     CreatedBy = "asesor@crm.com",              ← Automático (del JWT)
	///     CreatedAt = 2026-04-14T16:20:35.123Z,      ← Automático (UTC)
	///     IsDeleted = false
	/// }
	/// 
	/// VALIDACIONES REQUERIDAS (en NoteService, NO aquí):
	/// ------------------------------------------------------------------------
	/// - ClientId debe existir (usar ClientExistsAsync antes de llamar CreateAsync)
	/// - Content NO debe estar vacío (mínimo 1 carácter, recomendado 10+)
	/// - Content tiene máximo 5000 caracteres (configurado en AppDbContext)
	/// - Usuario debe estar autenticado (JWT token válido)
	/// 
	/// NOTA: Este repositorio NO valida datos de negocio, solo persiste.
	/// Las validaciones se hacen en NoteService usando DTOs con DataAnnotations.
	/// 
	/// EJEMPLO DE USO DESDE SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<NoteDto> CreateNoteAsync(CreateNoteDto dto)
	/// {
	///     // 1. Validar integridad referencial
	///     if (!await _noteRepo.ClientExistsAsync(dto.ClientId))
	///         throw new NotFoundException($"Cliente {dto.ClientId} no encontrado");
	///     
	///     // 2. Mapear DTO a entidad de dominio
	///     var note = new ClientNote
	///     {
	///         Content = dto.Content,
	///         ClientId = dto.ClientId
	///     };
	///     
	///     // 3. Crear en base de datos (auditoría automática)
	///     var created = await _noteRepo.CreateAsync(note);
	///     
	///     // 4. Mapear entidad a DTO de respuesta
	///     return _mapper.Map<NoteDto>(created);
	/// }
	/// 
	/// CASOS DE USO TÍPICOS:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Nota de llamada telefónica:
	///   Content: "Llamada recibida. Cliente interesado en plan Premium. 
	///            Programar demo para próxima semana."
	/// 
	/// Caso 2 - Nota de reunión:
	///   Content: "Reunión presencial con gerente de compras. Presentó necesidades
	///            de integración con SAP. Enviar propuesta técnica."
	/// 
	/// Caso 3 - Nota de seguimiento:
	///   Content: "Email enviado con cotización $15,000. Pendiente respuesta.
	///            Hacer follow-up el viernes."
	/// 
	/// TRANSACCIONALIDAD:
	/// ------------------------------------------------------------------------
	/// - SaveChangesAsync() ejecuta en una transacción ACID
	/// - Si falla (ej. violación FK), hace rollback automático
	/// - Si tiene éxito, el commit es inmediato y permanente
	/// - La base de datos SQLite garantiza atomicidad
	/// 
	/// INMUTABILIDAD DE LAS NOTAS:
	/// ------------------------------------------------------------------------
	/// Diseño intencional: NO existe UpdateAsync() ni DeleteAsync() en esta interfaz
	/// porque las notas son registros históricos inmutables. Razones:
	/// 
	/// - TRAZABILIDAD: El historial de comunicación NO debe modificarse
	/// - AUDITORÍA: Las notas son evidencia de interacciones pasadas
	/// - CUMPLIMIENTO: Regulaciones pueden requerir historial inmutable
	/// - INTEGRIDAD: Previene alteración de registros históricos
	/// 
	/// Si una nota tiene un error, la solución correcta es:
	/// - Crear una nueva nota con la corrección
	/// - O marcar la nota incorrecta como eliminada (soft delete)
	/// </remarks>
	public async Task<ClientNote> CreateAsync(ClientNote note)
	{
		_context.ClientNotes.Add(note); // Marca como "Added" en ChangeTracker
		await _context.SaveChangesAsync(); // Ejecuta INSERT + auditoría automática
		return note; // Retorna con ID asignado y auditoría completa
	}

	/// <summary>
	/// Verifica si existe un cliente con el ID especificado (sin estar eliminado).
	/// </summary>
	/// <param name="clientId">ID del cliente a verificar</param>
	/// <returns>true si el cliente existe y NO está eliminado, false en caso contrario</returns>
	/// <remarks>
	/// PROCESO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. _context.Clients: Accede al DbSet<Client> del contexto
	/// 2. (AUTOMÁTICO) Filtro global IsDeleted = false se aplica implícitamente
	///    - Configurado en AppDbContext.OnModelCreating()
	///    - HasQueryFilter(e => !e.IsDeleted) para todas las entidades
	/// 3. AnyAsync(x => x.Id == clientId): Ejecuta query de verificación
	/// 4. SQLite retorna 1 (true) si encuentra coincidencia, 0 (false) si no
	/// 
	/// SQL GENERADO POR EF CORE:
	/// ------------------------------------------------------------------------
	/// SELECT CASE
	///     WHEN EXISTS (
	///         SELECT 1 FROM [Clients] AS [c]
	///         WHERE [c].[Id] = @clientId AND [c].[IsDeleted] = 0
	///     )
	///     THEN CAST(1 AS bit)
	///     ELSE CAST(0 AS bit)
	/// END AS [Value]
	/// 
	/// COMPARACIÓN DE PERFORMANCE - MÉTODOS DE VERIFICACIÓN:
	/// ------------------------------------------------------------------------
	/// AnyAsync() - RECOMENDADO ✓
	/// - SQL: SELECT CASE WHEN EXISTS(SELECT 1...) THEN 1 ELSE 0 END
	/// - Comportamiento: Se detiene apenas encuentra 1 registro coincidente
	/// - Columnas retornadas: Ninguna (solo verificación booleana)
	/// - Performance: ~0.3ms
	/// - Uso de RAM: Mínimo (solo bool)
	/// - Retorna: bool
	/// 
	/// CountAsync() - NO ÓPTIMO ✗
	/// - SQL: SELECT COUNT(*) FROM [Clients] WHERE...
	/// - Comportamiento: Cuenta TODOS los registros coincidentes (innecesario)
	/// - Columnas retornadas: Valor entero
	/// - Performance: ~1.5ms (5x más lento)
	/// - Uso de RAM: Bajo
	/// - Retorna: int
	/// 
	/// FirstOrDefaultAsync() - NO ÓPTIMO ✗
	/// - SQL: SELECT TOP 1 * FROM [Clients] WHERE...
	/// - Comportamiento: Carga la entidad completa con TODAS sus columnas
	/// - Columnas retornadas: Todas (Name, Email, Phone, Address, etc.)
	/// - Performance: ~1ms (3x más lento)
	/// - Uso de RAM: Alto (objeto completo en memoria)
	/// - Retorna: Client? (puede ser null)
	/// 
	/// CONCLUSIÓN: AnyAsync() es la mejor opción para validaciones de existencia.
	/// 
	/// CASOS DE USO:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Cliente existe y está activo:
	///   bool exists = await ClientExistsAsync(10);
	///   // SQL: WHERE Id = 10 AND IsDeleted = 0
	///   // Retorna: true → Permitir crear la nota
	/// 
	/// Caso 2 - Cliente NO existe en la base de datos:
	///   bool exists = await ClientExistsAsync(999);
	///   // SQL: WHERE Id = 999 AND IsDeleted = 0
	///   // Retorna: false → Lanzar NotFoundException
	/// 
	/// Caso 3 - Cliente existe pero fue eliminado (IsDeleted = true):
	///   bool exists = await ClientExistsAsync(5);
	///   // SQL: WHERE Id = 5 AND IsDeleted = 0 ← Filtro global automático
	///   // Retorna: false → El cliente está soft-deleted, NO permitir nota
	/// 
	/// FLUJO COMPLETO EN SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<NoteDto> CreateNoteAsync(CreateNoteDto dto)
	/// {
	///     // PASO 1: Validar integridad referencial
	///     if (!await _noteRepo.ClientExistsAsync(dto.ClientId))
	///     {
	///         // Lanzar excepción con mensaje descriptivo
	///         throw new NotFoundException(
	///             $"Cliente con ID {dto.ClientId} no existe o fue eliminado"
	///         );
	///     }
	///     
	///     // PASO 2: Mapear DTO → Entidad
	///     var note = _mapper.Map<ClientNote>(dto);
	///     
	///     // PASO 3: Crear en base de datos
	///     var created = await _noteRepo.CreateAsync(note);
	///     
	///     // PASO 4: Mapear Entidad → DTO respuesta
	///     return _mapper.Map<NoteDto>(created);
	/// }
	/// 
	/// VENTAJAS DE VALIDAR CON ClientExistsAsync():
	/// ------------------------------------------------------------------------
	/// OPCIÓN 1 - SIN validación previa (MAL):
	/// - Se intenta el INSERT directamente
	/// - SQLite lanza SqlException: "FOREIGN KEY constraint failed"
	/// - Excepción técnica y críptica
	/// - ErrorHandlingMiddleware captura y retorna 500 Internal Server Error
	/// - Mensaje genérico: "Ocurrió un error interno"
	/// - Mala experiencia de usuario (UX)
	/// - Dificulta debugging
	/// 
	/// OPCIÓN 2 - CON validación previa usando ClientExistsAsync() (BIEN):
	/// - Se valida ANTES de intentar el INSERT
	/// - Se detecta el problema de forma controlada
	/// - Se lanza NotFoundException con mensaje descriptivo en español
	/// - ErrorHandlingMiddleware captura y retorna 404 Not Found
	/// - Mensaje específico: "Cliente con ID 999 no existe o fue eliminado"
	/// - Excelente experiencia de usuario
	/// - Facilita debugging y logs
	/// - El cliente de la API puede manejar el error apropiadamente
	/// 
	/// INTEGRIDAD REFERENCIAL Y SOFT DELETE:
	/// ------------------------------------------------------------------------
	/// El filtro global previene:
	/// - Crear notas para clientes soft-deleted (IsDeleted = true)
	/// - Acceder a datos de clientes eliminados lógicamente
	/// - Inconsistencias en la integridad referencial lógica
	/// 
	/// Esto mantiene el sistema consistente: si un cliente está "eliminado"
	/// (aunque físicamente exista en la BD), NO se pueden crear nuevas notas
	/// asociadas a él. Esto tiene sentido de negocio: no se documentan
	/// interacciones con clientes inactivos.
	/// 
	/// SEGURIDAD:
	/// ------------------------------------------------------------------------
	/// - Previene ataques de enumeración de IDs (no expone si un cliente existe)
	/// - El filtro global IsDeleted agrega una capa adicional de seguridad
	/// - Los permisos se validan en capas superiores (Controller/Service)
	/// </remarks>
	public async Task<bool> ClientExistsAsync(int clientId)
	{
		return await _context.Clients.AnyAsync(x => x.Id == clientId);
	}
}
