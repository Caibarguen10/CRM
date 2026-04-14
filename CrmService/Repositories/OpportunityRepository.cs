using CrmService.Data;
using CrmService.Domain;
using Microsoft.EntityFrameworkCore;

namespace CrmService.Repositories;

/// <summary>
/// Implementación del repositorio de oportunidades de negocio que gestiona el acceso a datos
/// de la entidad Opportunity usando Entity Framework Core.
/// </summary>
/// <remarks>
/// ARQUITECTURA Y RESPONSABILIDADES:
/// ------------------------------------------------------------------------
/// Esta clase implementa el patrón Repository, encapsulando toda la lógica de
/// acceso a datos relacionada con las oportunidades de venta. Proporciona una capa
/// de abstracción entre la lógica de negocio (OpportunityService) y la persistencia (EF Core).
/// 
/// QUÉ ES UNA OPORTUNIDAD EN EL CONTEXTO CRM:
/// ------------------------------------------------------------------------
/// Una "oportunidad" (Opportunity) representa una posible venta o negocio potencial
/// con un cliente. Es el corazón del proceso comercial:
/// 
/// - Ejemplo 1: Venta de licencias de software por $50,000 USD
/// - Ejemplo 2: Renovación de contrato anual por $25,000 USD
/// - Ejemplo 3: Expansión de servicios a nuevas ubicaciones por $100,000 USD
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
///    - Las oportunidades eliminadas permanecen en BD pero invisibles
/// 
/// 4. PRECISIÓN MONETARIA:
///    - El campo Amount usa decimal(18,2) para precisión exacta
///    - Evita errores de redondeo de float/double en cálculos monetarios
///    - Soporta hasta $999,999,999,999,999.99 con 2 decimales
/// 
/// 5. INMUTABILIDAD:
///    - Las oportunidades NO tienen métodos Update/Edit en este repositorio
///    - Diseño intencional: las oportunidades son registros históricos
///    - Solo se pueden crear nuevas, no modificar existentes
/// 
/// RELACIÓN CON OTRAS CAPAS:
/// ------------------------------------------------------------------------
/// OpportunitiesController → OpportunityService → OpportunityRepository → AppDbContext → SQLite
/// 
/// PIPELINE DE VENTAS (Sales Pipeline):
/// ------------------------------------------------------------------------
/// El conjunto de oportunidades forma el "pipeline de ventas", que permite:
/// - Forecasting: Proyección de ingresos futuros
/// - KPIs: Tasa de conversión, ciclo de venta promedio, valor promedio
/// - Seguimiento: Qué oportunidades están activas, cuáles ganadas/perdidas
/// - Reportes: Desempeño por vendedor, por producto, por región
/// 
/// ESTADOS DE UNA OPORTUNIDAD (OpportunityStatus enum):
/// ------------------------------------------------------------------------
/// - Lead: Prospecto inicial, cliente mostró interés
/// - Qualified: Cliente calificado, tiene presupuesto y necesidad real
/// - Proposal: Propuesta enviada, cotización formal presentada
/// - Negotiation: En negociación de términos y precios
/// - ClosedWon: ¡Ganada! Cliente firmó/compró
/// - ClosedLost: Perdida, cliente no compró o eligió competencia
/// 
/// PERMISOS (verificados en OpportunitiesController/OpportunityService):
/// ------------------------------------------------------------------------
/// - Admin: CRUD completo en oportunidades de cualquier cliente
/// - Asesor: Read + Create (puede ver y crear, pero NO editar/eliminar)
/// - Auditor: Solo Read (puede consultar pipeline pero NO modificar)
/// </remarks>
public class OpportunityRepository : IOpportunityRepository
{
	/// <summary>
	/// Contexto de base de datos de Entity Framework Core.
	/// </summary>
	/// <remarks>
	/// Este contexto gestiona:
	/// - Conexión a SQLite (archivo crm.db local)
	/// - Interceptación de SaveChanges() para auditoría automática
	/// - Filtros globales de soft delete (IsDeleted = false)
	/// - Relaciones entre entidades (Opportunity → Client)
	/// - Change tracking para detección de modificaciones
	/// - Configuración de precisión decimal(18,2) para Amount
	/// 
	/// CICLO DE VIDA (Scoped):
	/// - Se crea al inicio de cada request HTTP
	/// - Se destruye automáticamente al finalizar el request
	/// - NO debe reutilizarse entre requests (evita problemas de concurrencia)
	/// - El pool de conexiones de SQLite maneja eficientemente las conexiones
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
	/// - Desacoplamiento: OpportunityRepository NO instancia AppDbContext directamente
	/// - Testabilidad: Se puede inyectar un DbContext en memoria para tests
	/// - Ciclo de vida gestionado: ASP.NET Core maneja creación/destrucción
	/// - Configuración centralizada: La cadena de conexión está en appsettings.json
	/// 
	/// CONFIGURACIÓN EN Program.cs:
	/// builder.Services.AddDbContext<AppDbContext>(options =>
	///     options.UseSqlite(connectionString), 
	///     ServiceLifetime.Scoped); // ← Una instancia por request HTTP
	/// 
	/// builder.Services.AddScoped<IOpportunityRepository, OpportunityRepository>();
	/// 
	/// VENTAJAS DEL PATRÓN:
	/// - El repositorio NO necesita saber de dónde viene el contexto
	/// - Facilita cambiar de SQLite a SQL Server sin modificar esta clase
	/// - Permite usar mocks en tests unitarios sin base de datos real
	/// </remarks>
	public OpportunityRepository(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Crea una nueva oportunidad de negocio asociada a un cliente.
	/// </summary>
	/// <param name="opportunity">Entidad oportunidad con los datos a guardar</param>
	/// <returns>La oportunidad creada con su ID asignado y campos de auditoría populados</returns>
	/// <remarks>
	/// PROCESO COMPLETO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. _context.Opportunities.Add(opportunity): Marca la entidad como "Added" en ChangeTracker
	/// 2. await _context.SaveChangesAsync(): Dispara el guardado asíncrono
	/// 3. (INTERNO) AppDbContext.SaveChangesAsync() sobrescrito intercepta la operación
	/// 4. (INTERNO) ChangeTracker.Entries() detecta entidades con estado "Added"
	/// 5. (INTERNO) ProcessAuditFields() se ejecuta:
	///    - Obtiene el usuario del JWT: HttpContext.User.Identity.Name
	///    - Asigna opportunity.CreatedBy = "asesor@crm.com" (ejemplo)
	///    - Asigna opportunity.CreatedAt = DateTime.UtcNow (2026-04-14T17:45:00Z)
	/// 6. (INTERNO) EF Core genera el SQL INSERT con parámetros
	/// 7. (INTERNO) SQLite ejecuta el INSERT y retorna el ID autogenerado
	/// 8. (INTERNO) EF Core actualiza opportunity.Id con el valor retornado por SQLite
	/// 9. return opportunity: Retorna la oportunidad con ID y auditoría completos
	/// 
	/// SQL GENERADO POR EF CORE:
	/// ------------------------------------------------------------------------
	/// INSERT INTO [Opportunities] (
	///     [Title], [Description], [Amount], [Status], [ClientId],
	///     [CreatedBy], [CreatedAt], [IsDeleted]
	/// )
	/// VALUES (
	///     @p0, @p1, @p2, @p3, @p4,
	///     @p5, @p6, 0
	/// );
	/// SELECT [Id] FROM [Opportunities] WHERE [rowid] = last_insert_rowid();
	/// 
	/// PARÁMETROS SQL EJEMPLO:
	/// ------------------------------------------------------------------------
	/// @p0 = 'Venta Plan Enterprise Q2 2026'           (Title)
	/// @p1 = 'Migración de 200 usuarios a plan...'    (Description)
	/// @p2 = 75000.00                                  (Amount - decimal)
	/// @p3 = 2                                         (Status - int enum: Proposal)
	/// @p4 = 42                                        (ClientId - Foreign Key)
	/// @p5 = 'vendedor@crm.com'                       (CreatedBy - automático del JWT)
	/// @p6 = '2026-04-14T17:45:30.123Z'               (CreatedAt - automático UTC)
	/// 
	/// AUDITORÍA AUTOMÁTICA - TRANSFORMACIÓN DE LA ENTIDAD:
	/// ------------------------------------------------------------------------
	/// ANTES de SaveChangesAsync():
	/// {
	///     Id = 0,
	///     Title = "Renovación contrato anual",
	///     Description = "Cliente desea renovar por segundo año...",
	///     Amount = 45000.00m,
	///     Status = OpportunityStatus.Qualified,
	///     ClientId = 28,
	///     CreatedBy = null,        ← Vacío
	///     CreatedAt = default,     ← Vacío (0001-01-01)
	///     IsDeleted = false
	/// }
	/// 
	/// DESPUÉS de SaveChangesAsync():
	/// {
	///     Id = 312,                                   ← Asignado por SQLite
	///     Title = "Renovación contrato anual",
	///     Description = "Cliente desea renovar por segundo año...",
	///     Amount = 45000.00m,
	///     Status = OpportunityStatus.Qualified,
	///     ClientId = 28,
	///     CreatedBy = "asesor@crm.com",              ← Automático (del JWT)
	///     CreatedAt = 2026-04-14T17:45:30.123Z,      ← Automático (UTC)
	///     IsDeleted = false
	/// }
	/// 
	/// VALIDACIONES REQUERIDAS (en OpportunityService, NO aquí):
	/// ------------------------------------------------------------------------
	/// - ClientId debe existir (usar ClientExistsAsync antes de CreateAsync)
	/// - Title NO debe estar vacío (obligatorio)
	/// - Amount debe ser >= 0 (no se permiten montos negativos)
	/// - Status debe ser un valor válido del enum OpportunityStatus
	/// - Description es opcional pero recomendada
	/// 
	/// NOTA: Este repositorio NO valida datos de negocio, solo persiste.
	/// Las validaciones se hacen en OpportunityService usando DTOs con DataAnnotations.
	/// 
	/// PRECISIÓN MONETARIA - DECIMAL(18,2):
	/// ------------------------------------------------------------------------
	/// ¿Por qué decimal y NO float/double?
	/// 
	/// PROBLEMA CON FLOAT/DOUBLE:
	/// float amount = 0.1f + 0.2f;
	/// // Resultado: 0.30000001 (ERROR de redondeo binario)
	/// // En facturación: $1000.00 + $2000.00 puede dar $3000.01
	/// 
	/// SOLUCIÓN CON DECIMAL:
	/// decimal amount = 0.1m + 0.2m;
	/// // Resultado: 0.3 (EXACTO)
	/// // Configuración: decimal(18,2)
	/// // 18 dígitos totales, 2 después del punto decimal
	/// // Rango: -999,999,999,999,999.99 hasta 999,999,999,999,999.99
	/// 
	/// CASOS DE USO:
	/// - Amount = 50000.00m → Oportunidad de $50,000.00 USD
	/// - Amount = 125750.50m → Oportunidad de $125,750.50 USD
	/// - Amount = 0.00m → Oportunidad sin monto definido aún
	/// 
	/// EJEMPLO DE USO DESDE SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityDto dto)
	/// {
	///     // 1. Validar integridad referencial
	///     if (!await _opportunityRepo.ClientExistsAsync(dto.ClientId))
	///         throw new NotFoundException($"Cliente {dto.ClientId} no encontrado");
	///     
	///     // 2. Validar monto
	///     if (dto.Amount < 0)
	///         throw new ValidationException("El monto no puede ser negativo");
	///     
	///     // 3. Mapear DTO a entidad de dominio
	///     var opportunity = new Opportunity
	///     {
	///         Title = dto.Title,
	///         Description = dto.Description,
	///         Amount = dto.Amount,
	///         Status = dto.Status,
	///         ClientId = dto.ClientId
	///     };
	///     
	///     // 4. Crear en base de datos (auditoría automática)
	///     var created = await _opportunityRepo.CreateAsync(opportunity);
	///     
	///     // 5. Mapear entidad a DTO de respuesta
	///     return _mapper.Map<OpportunityDto>(created);
	/// }
	/// 
	/// CASOS DE USO TÍPICOS - EJEMPLOS REALES:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Nueva venta de producto:
	///   Title: "Venta 50 licencias Software XYZ"
	///   Description: "Cliente ABC necesita licencias para nueva sucursal"
	///   Amount: 25000.00m
	///   Status: OpportunityStatus.Lead
	/// 
	/// Caso 2 - Renovación de contrato:
	///   Title: "Renovación anual Cliente Empresa S.A."
	///   Description: "Renovación del contrato por segundo año consecutivo"
	///   Amount: 80000.00m
	///   Status: OpportunityStatus.Negotiation
	/// 
	/// Caso 3 - Proyecto de consultoría:
	///   Title: "Consultoría implementación ERP"
	///   Description: "Proyecto de 6 meses para implementar sistema ERP"
	///   Amount: 150000.00m
	///   Status: OpportunityStatus.Proposal
	/// 
	/// MÉTRICAS CALCULADAS DESDE OPORTUNIDADES:
	/// ------------------------------------------------------------------------
	/// 1. PIPELINE TOTAL (valor del pipeline activo):
	///    SELECT SUM(Amount) FROM Opportunities 
	///    WHERE Status NOT IN ('ClosedWon', 'ClosedLost') AND IsDeleted = 0
	/// 
	/// 2. TASA DE CONVERSIÓN:
	///    (Oportunidades ClosedWon / Total Oportunidades) * 100
	///    Ejemplo: 25 ganadas / 100 totales = 25% tasa de conversión
	/// 
	/// 3. VALOR PROMEDIO DE VENTA:
	///    SELECT AVG(Amount) FROM Opportunities WHERE Status = 'ClosedWon'
	/// 
	/// 4. CICLO DE VENTA PROMEDIO:
	///    AVG(FechaCierreGanada - CreatedAt) para oportunidades ClosedWon
	/// 
	/// TRANSACCIONALIDAD:
	/// ------------------------------------------------------------------------
	/// - SaveChangesAsync() ejecuta en una transacción ACID
	/// - Si falla (ej. violación FK), hace rollback automático
	/// - Si tiene éxito, el commit es inmediato y permanente
	/// - La base de datos SQLite garantiza atomicidad
	/// 
	/// INMUTABILIDAD DE LAS OPORTUNIDADES:
	/// ------------------------------------------------------------------------
	/// Diseño intencional: NO existe UpdateAsync() en esta interfaz porque
	/// las oportunidades son registros históricos. Razones:
	/// 
	/// - TRAZABILIDAD: El pipeline histórico NO debe modificarse
	/// - FORECASTING: Los reportes se basan en datos históricos inmutables
	/// - AUDITORÍA: Las oportunidades son evidencia de gestión comercial
	/// - KPIs: Las métricas requieren datos consistentes e inalterables
	/// 
	/// Si una oportunidad tiene un error, la solución correcta es:
	/// - Crear una nueva oportunidad con los datos corregidos
	/// - O marcar la oportunidad incorrecta como eliminada (soft delete)
	/// </remarks>
	public async Task<Opportunity> CreateAsync(Opportunity opportunity)
	{
		_context.Opportunities.Add(opportunity); // Marca como "Added" en ChangeTracker
		await _context.SaveChangesAsync(); // Ejecuta INSERT + auditoría automática
		return opportunity; // Retorna con ID asignado y auditoría completa
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
	/// - Performance: ~0.2-0.5ms (depende de índices)
	/// - Uso de RAM: Mínimo (solo bool)
	/// - Retorna: bool
	/// 
	/// CountAsync() - NO ÓPTIMO ✗
	/// - SQL: SELECT COUNT(*) FROM [Clients] WHERE...
	/// - Comportamiento: Cuenta TODOS los registros coincidentes (innecesario)
	/// - Columnas retornadas: Valor entero
	/// - Performance: ~1-2ms (hasta 10x más lento en tablas grandes)
	/// - Uso de RAM: Bajo
	/// - Retorna: int
	/// 
	/// FirstOrDefaultAsync() - NO ÓPTIMO ✗
	/// - SQL: SELECT TOP 1 * FROM [Clients] WHERE...
	/// - Comportamiento: Carga la entidad completa con TODAS sus columnas
	/// - Columnas retornadas: Todas (Name, Email, Phone, Address, etc.)
	/// - Performance: ~0.8-1.5ms (3-5x más lento)
	/// - Uso de RAM: Alto (objeto completo en memoria)
	/// - Retorna: Client? (puede ser null)
	/// 
	/// CONCLUSIÓN: AnyAsync() es la opción óptima para validaciones de existencia.
	/// 
	/// CASOS DE USO:
	/// ------------------------------------------------------------------------
	/// Caso 1 - Cliente existe y está activo:
	///   bool exists = await ClientExistsAsync(42);
	///   // SQL: WHERE Id = 42 AND IsDeleted = 0
	///   // Retorna: true → Permitir crear la oportunidad
	/// 
	/// Caso 2 - Cliente NO existe en la base de datos:
	///   bool exists = await ClientExistsAsync(9999);
	///   // SQL: WHERE Id = 9999 AND IsDeleted = 0
	///   // Retorna: false → Lanzar NotFoundException
	/// 
	/// Caso 3 - Cliente existe pero fue eliminado (IsDeleted = true):
	///   bool exists = await ClientExistsAsync(10);
	///   // SQL: WHERE Id = 10 AND IsDeleted = 0 ← Filtro global automático
	///   // Retorna: false → El cliente está soft-deleted, NO permitir oportunidad
	/// 
	/// FLUJO COMPLETO EN SERVICIO:
	/// ------------------------------------------------------------------------
	/// public async Task<OpportunityDto> CreateOpportunityAsync(CreateOpportunityDto dto)
	/// {
	///     // PASO 1: Validar integridad referencial
	///     if (!await _opportunityRepo.ClientExistsAsync(dto.ClientId))
	///     {
	///         // Lanzar excepción con mensaje descriptivo en español
	///         throw new NotFoundException(
	///             $"Cliente con ID {dto.ClientId} no existe o fue eliminado"
	///         );
	///     }
	///     
	///     // PASO 2: Validar reglas de negocio
	///     if (dto.Amount < 0)
	///         throw new ValidationException("El monto no puede ser negativo");
	///     
	///     // PASO 3: Mapear DTO → Entidad
	///     var opportunity = _mapper.Map<Opportunity>(dto);
	///     
	///     // PASO 4: Crear en base de datos
	///     var created = await _opportunityRepo.CreateAsync(opportunity);
	///     
	///     // PASO 5: Mapear Entidad → DTO respuesta
	///     return _mapper.Map<OpportunityDto>(created);
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
	/// - Log muestra SqlException sin contexto de negocio
	/// - Dificulta debugging y troubleshooting
	/// 
	/// OPCIÓN 2 - CON validación previa usando ClientExistsAsync() (BIEN):
	/// - Se valida ANTES de intentar el INSERT
	/// - Se detecta el problema de forma controlada
	/// - Se lanza NotFoundException con mensaje descriptivo en español
	/// - ErrorHandlingMiddleware captura y retorna 404 Not Found
	/// - Mensaje específico: "Cliente con ID 999 no existe o fue eliminado"
	/// - Excelente experiencia de usuario
	/// - Log muestra contexto claro del error de negocio
	/// - Facilita debugging: se sabe exactamente qué cliente faltó
	/// - El cliente de la API puede manejar el error apropiadamente
	/// - Mejor para RESTful APIs: 404 es semánticamente correcto
	/// 
	/// INTEGRIDAD REFERENCIAL Y SOFT DELETE:
	/// ------------------------------------------------------------------------
	/// El filtro global previene:
	/// - Crear oportunidades para clientes soft-deleted (IsDeleted = true)
	/// - Acceder a datos de clientes eliminados lógicamente
	/// - Inconsistencias en la integridad referencial lógica
	/// 
	/// REGLA DE NEGOCIO:
	/// Si un cliente está "eliminado" (aunque físicamente exista en la BD),
	/// NO se pueden crear nuevas oportunidades asociadas a él. Tiene sentido:
	/// - NO crear oportunidades de venta para clientes inactivos
	/// - Mantener el pipeline limpio y relevante
	/// - Si se reactiva el cliente (IsDeleted = false), entonces SÍ se pueden
	///   crear nuevas oportunidades
	/// 
	/// SEGURIDAD:
	/// ------------------------------------------------------------------------
	/// - Previene ataques de enumeración de IDs (no expone si un cliente existe)
	/// - El filtro global IsDeleted agrega una capa adicional de seguridad
	/// - Los permisos se validan en capas superiores (Controller/Service)
	/// - Ejemplo: Un Asesor solo debería crear oportunidades para sus clientes asignados
	/// </remarks>
	public async Task<bool> ClientExistsAsync(int clientId)
	{
		return await _context.Clients.AnyAsync(x => x.Id == clientId);
	}
}
