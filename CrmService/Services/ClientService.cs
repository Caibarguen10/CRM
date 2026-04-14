using AutoMapper;
using CrmService.DTOs;
using CrmService.Repositories;
using CrmService.Domain;
using CrmService.Common;
using Microsoft.Extensions.Logging;

namespace CrmService.Services;

/// <summary>
/// Servicio de clientes que implementa la lógica de negocio para la gestión de clientes.
/// </summary>
/// <remarks>
/// ARQUITECTURA - CAPA DE SERVICIOS (Business Logic Layer):
/// ------------------------------------------------------------------------
/// Esta clase es el núcleo de la lógica de negocio para clientes. Actúa como intermediario
/// entre los controladores (Presentation Layer) y los repositorios (Data Access Layer).
/// 
/// RESPONSABILIDADES:
/// ------------------------------------------------------------------------
/// 1. VALIDACIONES DE NEGOCIO:
///    - Verificar unicidad de DocumentNumber al crear/actualizar clientes
///    - Validar reglas de negocio complejas
///    - Prevenir operaciones inválidas antes de llegar a la base de datos
/// 
/// 2. ORQUESTACIÓN:
///    - Coordinar llamadas al repositorio
///    - Gestionar el flujo completo de cada operación CRUD
///    - Manejar casos especiales (duplicados, no encontrados, etc.)
/// 
/// 3. TRANSFORMACIÓN DE DATOS:
///    - Mapear DTOs → Entidades de dominio (para persistir)
///    - Mapear Entidades → DTOs (para respuestas HTTP)
///    - Usar AutoMapper para conversiones automáticas
/// 
/// 4. LOGGING Y OBSERVABILIDAD:
///    - Registrar todas las operaciones importantes
///    - Log de información para operaciones exitosas
///    - Log de warnings para casos excepcionales (no encontrado, duplicado)
///    - Facilitar debugging y auditoría
/// 
/// 5. ABSTRACCIÓN:
///    - Ocultar detalles de implementación (repositorio, EF Core, SQLite)
///    - Proporcionar una API limpia orientada a casos de uso de negocio
///    - Los controladores NO conocen sobre repositorios ni DbContext
/// 
/// PATRÓN DE DISEÑO - DEPENDENCY INJECTION:
/// ------------------------------------------------------------------------
/// Este servicio recibe sus dependencias por constructor:
/// - IClientRepository: Para acceso a datos
/// - IMapper: Para conversiones DTO ↔ Entity
/// - ILogger: Para logging estructurado
/// 
/// Ciclo de vida: Scoped (una instancia por request HTTP)
/// 
/// FLUJO DE UNA OPERACIÓN TÍPICA (Ejemplo: CreateAsync):
/// ------------------------------------------------------------------------
/// 1. Controller recibe HTTP POST con JSON
/// 2. Model Binding convierte JSON a CreateClientDto
/// 3. Controller llama a _clientService.CreateAsync(dto)
/// 4. Service valida reglas de negocio (no duplicado)
/// 5. Service mapea DTO → Entity con AutoMapper
/// 6. Service llama a _repository.CreateAsync(entity)
/// 7. Repository persiste en SQLite con auditoría automática
/// 8. Repository retorna Entity con ID asignado
/// 9. Service mapea Entity → ClientDto
/// 10. Service retorna ClientDto al Controller
/// 11. Controller retorna HTTP 201 Created con ClientDto en JSON
/// 
/// VENTAJAS DE ESTA ARQUITECTURA:
/// ------------------------------------------------------------------------
/// - TESTABILIDAD: Se pueden crear mocks de IClientRepository en tests unitarios
/// - REUTILIZACIÓN: Múltiples controllers pueden usar este mismo servicio
/// - MANTENIBILIDAD: Toda la lógica de negocio está centralizada aquí
/// - SEPARACIÓN: Cada capa tiene responsabilidad única (SRP)
/// - ESCALABILIDAD: Fácil agregar caché, validaciones, o lógica compleja
/// 
/// LOGGING ESTRUCTURADO:
/// ------------------------------------------------------------------------
/// Se usa ILogger<ClientService> con structured logging:
/// - Parámetros con nombres: {ClientId}, {DocumentNumber}, {Page}, {PageSize}
/// - Facilita búsquedas en sistemas de logging (ELK, Splunk, Application Insights)
/// - Permite análisis y métricas de uso
/// 
/// PERMISOS (validados en ClientsController, NO aquí):
/// ------------------------------------------------------------------------
/// - Admin: CRUD completo
/// - Asesor: Read (GET)
/// - Auditor: Read (GET)
/// </remarks>
public class ClientService : IClientService
{
	/// <summary>
	/// Repositorio de clientes para acceso a datos.
	/// </summary>
	private readonly IClientRepository _repository;

	/// <summary>
	/// AutoMapper para conversiones automáticas entre DTOs y Entidades.
	/// </summary>
	private readonly IMapper _mapper;

	/// <summary>
	/// Logger para registro estructurado de operaciones.
	/// </summary>
	private readonly ILogger<ClientService> _logger;

	/// <summary>
	/// Constructor que recibe las dependencias por inyección.
	/// </summary>
	/// <param name="repository">Repositorio de clientes</param>
	/// <param name="mapper">AutoMapper configurado</param>
	/// <param name="logger">Logger para ClientService</param>
	/// <remarks>
	/// INYECCIÓN DE DEPENDENCIAS (Dependency Injection):
	/// ------------------------------------------------------------------------
	/// ASP.NET Core inyecta automáticamente estas dependencias al crear el servicio.
	/// 
	/// CONFIGURACIÓN EN Program.cs:
	/// builder.Services.AddScoped<IClientRepository, ClientRepository>();
	/// builder.Services.AddScoped<IClientService, ClientService>();
	/// builder.Services.AddAutoMapper(typeof(MappingProfile));
	/// // ILogger se configura automáticamente
	/// 
	/// CICLO DE VIDA (Scoped):
	/// - Se crea una instancia al inicio de cada request HTTP
	/// - Se destruye automáticamente al finalizar el request
	/// - El mismo servicio se reutiliza en todo el request si se inyecta múltiples veces
	/// </remarks>
	public ClientService(IClientRepository repository, IMapper mapper, ILogger<ClientService> logger)
	{
		_repository = repository;
		_mapper = mapper;
		_logger = logger;
	}

	/// <summary>
	/// Obtiene una lista paginada de clientes con filtros opcionales.
	/// </summary>
	/// <param name="pagination">Parámetros de paginación (página, tamaño)</param>
	/// <param name="filters">Filtros opcionales (nombre, email, documento)</param>
	/// <returns>Resultado paginado con lista de clientes y metadata</returns>
	/// <remarks>
	/// PROCESO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. Registra log de información con parámetros de paginación
	/// 2. Llama al repositorio para obtener clientes paginados
	/// 3. El repositorio retorna PagedResult<Client> (entidades de dominio)
	/// 4. Mapea List<Client> → List<ClientDto> con AutoMapper
	/// 5. Construye PagedResult<ClientDto> con items mapeados y metadata
	/// 6. Retorna el resultado al controller
	/// 
	/// LOGGING ESTRUCTURADO:
	/// ------------------------------------------------------------------------
	/// _logger.LogInformation("Obteniendo clientes. Página: {Page}, Tamaño: {PageSize}", 
	///     pagination.Page, pagination.PageSize);
	/// 
	/// Ventajas:
	/// - Los parámetros {Page} y {PageSize} se indexan automáticamente
	/// - Facilita búsquedas: "Mostrar todos los logs donde Page > 5"
	/// - Permite análisis de patrones de uso
	/// 
	/// MAPEO CON AUTOMAPPER:
	/// ------------------------------------------------------------------------
	/// Items = _mapper.Map<List<ClientDto>>(pagedClients.Items)
	/// 
	/// AutoMapper convierte automáticamente List<Client> a List<ClientDto> usando
	/// las reglas definidas en MappingProfile.cs:
	/// 
	/// CreateMap<Client, ClientDto>();
	/// 
	/// Campos mapeados automáticamente (mismos nombres):
	/// - Id → Id
	/// - Name → Name
	/// - Email → Email
	/// - Phone → Phone
	/// - Address → Address
	/// - DocumentNumber → DocumentNumber
	/// - CreatedAt → CreatedAt
	/// - CreatedBy → CreatedBy
	/// 
	/// PAGINACIÓN:
	/// ------------------------------------------------------------------------
	/// Ejemplo: pagination = { Page = 2, PageSize = 10 }
	/// 
	/// El repositorio:
	/// - Cuenta total de clientes que cumplen filtros: TotalCount = 45
	/// - Calcula Skip: (Page - 1) * PageSize = (2 - 1) * 10 = 10
	/// - Ejecuta query: .Skip(10).Take(10)
	/// - Retorna clientes del 11 al 20
	/// 
	/// Respuesta:
	/// {
	///     Items: [ /* 10 clientes */ ],
	///     TotalCount: 45,
	///     Page: 2,
	///     PageSize: 10
	/// }
	/// 
	/// FILTROS OPCIONALES:
	/// ------------------------------------------------------------------------
	/// Si filters = null:
	/// - Se retornan TODOS los clientes (paginados)
	/// 
	/// Si filters = { SearchTerm = "Acme" }:
	/// - Se filtran clientes donde:
	///   * Name LIKE '%Acme%' OR
	///   * Email LIKE '%Acme%' OR
	///   * DocumentNumber LIKE '%Acme%'
	/// 
	/// SOFT DELETE:
	/// ------------------------------------------------------------------------
	/// Los clientes eliminados (IsDeleted = true) NO aparecen en los resultados
	/// gracias al filtro global en AppDbContext.
	/// 
	/// EJEMPLO DE USO DESDE CONTROLLER:
	/// ------------------------------------------------------------------------
	/// [HttpGet]
	/// public async Task<ActionResult<ApiResponse>> GetAll(
	///     [FromQuery] int page = 1,
	///     [FromQuery] int pageSize = 10,
	///     [FromQuery] string? searchTerm = null)
	/// {
	///     var pagination = new PaginationParams { Page = page, PageSize = pageSize };
	///     var filters = new ClientFilterDto { SearchTerm = searchTerm };
	///     
	///     var result = await _clientService.GetAllAsync(pagination, filters);
	///     
	///     return Ok(new ApiResponse 
	///     {
	///         Success = true,
	///         Data = result
	///     });
	/// }
	/// 
	/// RESPUESTA HTTP:
	/// ------------------------------------------------------------------------
	/// GET /api/clients?page=1&pageSize=10&searchTerm=Acme
	/// 
	/// 200 OK
	/// {
	///     "success": true,
	///     "data": {
	///         "items": [
	///             {
	///                 "id": 1,
	///                 "name": "Acme Corporation",
	///                 "email": "contact@acme.com",
	///                 "documentNumber": "12345678",
	///                 "createdAt": "2026-04-14T10:30:00Z"
	///             }
	///         ],
	///         "totalCount": 1,
	///         "page": 1,
	///         "pageSize": 10
	///     }
	/// }
	/// </remarks>
	public async Task<PagedResult<ClientDto>> GetAllAsync(PaginationParams pagination, ClientFilterDto? filters = null)
	{
		_logger.LogInformation("Obteniendo clientes. Página: {Page}, Tamaño: {PageSize}", pagination.Page, pagination.PageSize);
		
		var pagedClients = await _repository.GetAllAsync(pagination, filters);

		return new PagedResult<ClientDto>
		{
			Items = _mapper.Map<List<ClientDto>>(pagedClients.Items),
			TotalCount = pagedClients.TotalCount,
			Page = pagedClients.Page,
			PageSize = pagedClients.PageSize
		};
	}

	/// <summary>
	/// Obtiene un cliente específico por su ID.
	/// </summary>
	/// <param name="id">ID del cliente a buscar</param>
	/// <returns>DTO del cliente si existe, null si no se encuentra</returns>
	/// <remarks>
	/// PROCESO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. Registra log de información con el ID buscado
	/// 2. Llama al repositorio para buscar el cliente
	/// 3. Si NO existe (null):
	///    - Registra warning con el ID
	///    - Retorna null al controller
	/// 4. Si existe:
	///    - Mapea Client → ClientDto con AutoMapper
	///    - Retorna el ClientDto
	/// 
	/// LOGGING:
	/// ------------------------------------------------------------------------
	/// Operación exitosa:
	/// _logger.LogInformation("Obteniendo cliente por ID: {ClientId}", id);
	/// // Log: "Obteniendo cliente por ID: 42"
	/// 
	/// Cliente no encontrado:
	/// _logger.LogWarning("Cliente no encontrado. ID: {ClientId}", id);
	/// // Log: "Cliente no encontrado. ID: 999"
	/// 
	/// NIVELES DE LOG:
	/// - LogInformation: Operaciones normales y exitosas
	/// - LogWarning: Situaciones inusuales pero no errores (no encontrado)
	/// - LogError: Errores y excepciones
	/// 
	/// MAPEO CON AUTOMAPPER:
	/// ------------------------------------------------------------------------
	/// _mapper.Map<ClientDto>(client);
	/// 
	/// Convierte la entidad Client a ClientDto copiando las propiedades:
	/// 
	/// Client (entidad de dominio):
	/// {
	///     Id = 42,
	///     Name = "Acme Corp",
	///     Email = "contact@acme.com",
	///     DocumentNumber = "12345678",
	///     CreatedBy = "admin@crm.com",
	///     CreatedAt = 2026-04-14T10:30:00Z,
	///     IsDeleted = false,  ← NO se incluye en el DTO
	///     DeletedBy = null,   ← NO se incluye en el DTO
	///     DeletedAt = null    ← NO se incluye en el DTO
	/// }
	/// 
	/// ClientDto (para respuesta HTTP):
	/// {
	///     Id = 42,
	///     Name = "Acme Corp",
	///     Email = "contact@acme.com",
	///     DocumentNumber = "12345678",
	///     CreatedBy = "admin@crm.com",
	///     CreatedAt = "2026-04-14T10:30:00Z"
	/// }
	/// 
	/// VENTAJAS DE RETORNAR NULL:
	/// ------------------------------------------------------------------------
	/// El controller puede manejar el caso apropiadamente:
	/// 
	/// var client = await _clientService.GetByIdAsync(id);
	/// if (client == null)
	///     return NotFound(new ApiResponse 
	///     {
	///         Success = false,
	///         Message = "Cliente no encontrado"
	///     });
	/// 
	/// return Ok(new ApiResponse 
	/// {
	///     Success = true,
	///     Data = client
	/// });
	/// 
	/// ALTERNATIVA (lanzar excepción):
	/// Algunos diseños prefieren lanzar NotFoundException aquí y manejarla
	/// en ErrorHandlingMiddleware. Ambos enfoques son válidos.
	/// 
	/// SOFT DELETE:
	/// ------------------------------------------------------------------------
	/// Si el cliente está eliminado (IsDeleted = true), el repositorio retorna null
	/// debido al filtro global. Esto es correcto: un cliente eliminado NO debe
	/// ser accesible por su ID.
	/// 
	/// EJEMPLO DE USO DESDE CONTROLLER:
	/// ------------------------------------------------------------------------
	/// [HttpGet("{id}")]
	/// public async Task<ActionResult<ApiResponse>> GetById(int id)
	/// {
	///     var client = await _clientService.GetByIdAsync(id);
	///     
	///     if (client == null)
	///         return NotFound(new ApiResponse 
	///         {
	///             Success = false,
	///             Message = $"Cliente con ID {id} no encontrado"
	///         });
	///     
	///     return Ok(new ApiResponse 
	///     {
	///         Success = true,
	///         Data = client
	///     });
	/// }
	/// 
	/// RESPUESTAS HTTP:
	/// ------------------------------------------------------------------------
	/// Cliente encontrado:
	/// GET /api/clients/42
	/// 200 OK
	/// {
	///     "success": true,
	///     "data": {
	///         "id": 42,
	///         "name": "Acme Corp",
	///         "email": "contact@acme.com",
	///         "documentNumber": "12345678"
	///     }
	/// }
	/// 
	/// Cliente no encontrado:
	/// GET /api/clients/999
	/// 404 Not Found
	/// {
	///     "success": false,
	///     "message": "Cliente con ID 999 no encontrado"
	/// }
	/// </remarks>
	public async Task<ClientDto?> GetByIdAsync(int id)
	{
		_logger.LogInformation("Obteniendo cliente por ID: {ClientId}", id);
		
		var client = await _repository.GetByIdAsync(id);
		if (client == null)
		{
			_logger.LogWarning("Cliente no encontrado. ID: {ClientId}", id);
			return null;
		}

		return _mapper.Map<ClientDto>(client);
	}

	/// <summary>
	/// Crea un nuevo cliente en el sistema.
	/// </summary>
	/// <param name="dto">DTO con los datos del cliente a crear</param>
	/// <returns>DTO del cliente creado con su ID asignado</returns>
	/// <exception cref="InvalidOperationException">Si ya existe un cliente con ese número de documento</exception>
	/// <remarks>
	/// PROCESO COMPLETO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. Registra log con DocumentNumber del cliente a crear
	/// 2. VALIDACIÓN DE NEGOCIO: Verifica que no exista cliente con ese DocumentNumber
	/// 3. Si existe duplicado:
	///    - Registra warning
	///    - Lanza InvalidOperationException
	/// 4. Si NO hay duplicado:
	///    - Mapea CreateClientDto → Client con AutoMapper
	///    - Llama al repositorio para persistir
	///    - El repositorio ejecuta INSERT con auditoría automática
	///    - El repositorio retorna Client con ID asignado
	/// 5. Registra log de éxito con ID y DocumentNumber
	/// 6. Mapea Client → ClientDto
	/// 7. Retorna ClientDto al controller
	/// 
	/// VALIDACIÓN DE NEGOCIO - UNICIDAD DE DOCUMENTO:
	/// ------------------------------------------------------------------------
	/// var existingClient = await _repository.GetByDocumentAsync(dto.DocumentNumber);
	/// if (existingClient != null)
	///     throw new InvalidOperationException("El cliente ya existe.");
	/// 
	/// Esta validación es CRÍTICA para mantener integridad:
	/// - Previene clientes duplicados con mismo DNI/RUC/CUIT
	/// - Es una regla de negocio que NO se puede validar en el DTO
	/// - Requiere consultar la base de datos
	/// 
	/// MAPEO DTO → ENTIDAD:
	/// ------------------------------------------------------------------------
	/// var client = _mapper.Map<Client>(dto);
	/// 
	/// CreateClientDto:
	/// {
	///     Name = "Acme Corporation",
	///     DocumentNumber = "12345678",
	///     Email = "contact@acme.com",
	///     Phone = "+1234567890",
	///     Address = "123 Main St"
	/// }
	/// 
	/// Client (entidad):
	/// {
	///     Id = 0,  ← Será asignado por la BD
	///     Name = "Acme Corporation",
	///     DocumentNumber = "12345678",
	///     Email = "contact@acme.com",
	///     Phone = "+1234567890",
	///     Address = "123 Main St",
	///     CreatedBy = null,  ← Será asignado por AppDbContext
	///     CreatedAt = default,  ← Será asignado por AppDbContext
	///     IsDeleted = false
	/// }
	/// 
	/// AUDITORÍA AUTOMÁTICA (en AppDbContext.SaveChangesAsync):
	/// ------------------------------------------------------------------------
	/// Después del INSERT, la entidad queda:
	/// {
	///     Id = 42,  ← Asignado por SQLite
	///     Name = "Acme Corporation",
	///     DocumentNumber = "12345678",
	///     Email = "contact@acme.com",
	///     CreatedBy = "admin@crm.com",  ← Del JWT
	///     CreatedAt = 2026-04-14T10:30:00Z,  ← UTC
	///     IsDeleted = false
	/// }
	/// 
	/// LOGGING ESTRUCTURADO:
	/// ------------------------------------------------------------------------
	/// Inicio:
	/// _logger.LogInformation("Creando nuevo cliente. Documento: {DocumentNumber}", dto.DocumentNumber);
	/// 
	/// Duplicado detectado:
	/// _logger.LogWarning("Intento de crear cliente duplicado. Documento: {DocumentNumber}", dto.DocumentNumber);
	/// 
	/// Éxito:
	/// _logger.LogInformation("Cliente creado exitosamente. ID: {ClientId}, Documento: {DocumentNumber}", 
	///     createdClient.Id, createdClient.DocumentNumber);
	/// 
	/// MANEJO DE EXCEPCIÓN EN CONTROLLER:
	/// ------------------------------------------------------------------------
	/// [HttpPost]
	/// public async Task<ActionResult<ApiResponse>> Create(CreateClientDto dto)
	/// {
	///     try
	///     {
	///         var created = await _clientService.CreateAsync(dto);
	///         return CreatedAtAction(nameof(GetById), new { id = created.Id }, 
	///             new ApiResponse { Success = true, Data = created });
	///     }
	///     catch (InvalidOperationException ex)
	///     {
	///         return Conflict(new ApiResponse 
	///         {
	///             Success = false,
	///             Message = ex.Message  // "El cliente ya existe."
	///         });
	///     }
	/// }
	/// 
	/// ALTERNATIVA - ErrorHandlingMiddleware:
	/// El middleware puede capturar InvalidOperationException y retornar 409 Conflict
	/// automáticamente, evitando try-catch en cada controller.
	/// 
	/// RESPUESTAS HTTP:
	/// ------------------------------------------------------------------------
	/// Éxito:
	/// POST /api/clients
	/// Body: { "name": "Acme Corp", "documentNumber": "12345678", ... }
	/// 
	/// 201 Created
	/// Location: /api/clients/42
	/// {
	///     "success": true,
	///     "data": {
	///         "id": 42,
	///         "name": "Acme Corp",
	///         "documentNumber": "12345678",
	///         "createdBy": "admin@crm.com",
	///         "createdAt": "2026-04-14T10:30:00Z"
	///     }
	/// }
	/// 
	/// Duplicado:
	/// POST /api/clients
	/// Body: { "name": "Otro", "documentNumber": "12345678", ... }  ← Documento ya existe
	/// 
	/// 409 Conflict
	/// {
	///     "success": false,
	///     "message": "El cliente ya existe."
	/// }
	/// </remarks>
	public async Task<ClientDto> CreateAsync(CreateClientDto dto)
	{
		_logger.LogInformation("Creando nuevo cliente. Documento: {DocumentNumber}", dto.DocumentNumber);
		
		var existingClient = await _repository.GetByDocumentAsync(dto.DocumentNumber);
		if (existingClient != null)
		{
			_logger.LogWarning("Intento de crear cliente duplicado. Documento: {DocumentNumber}", dto.DocumentNumber);
			throw new InvalidOperationException("El cliente ya existe.");
		}

		var client = _mapper.Map<Client>(dto);
		var createdClient = await _repository.CreateAsync(client);

		_logger.LogInformation("Cliente creado exitosamente. ID: {ClientId}, Documento: {DocumentNumber}", 
			createdClient.Id, createdClient.DocumentNumber);

		return _mapper.Map<ClientDto>(createdClient);
	}

	/// <summary>
	/// Actualiza un cliente existente.
	/// </summary>
	/// <param name="id">ID del cliente a actualizar</param>
	/// <param name="dto">DTO con los nuevos datos del cliente</param>
	/// <returns>DTO del cliente actualizado, null si no se encuentra</returns>
	/// <exception cref="InvalidOperationException">Si el nuevo DocumentNumber ya está en uso por otro cliente</exception>
	/// <remarks>
	/// PROCESO COMPLETO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. Registra log con el ID del cliente a actualizar
	/// 2. Busca el cliente existente en la base de datos
	/// 3. Si NO existe:
	///    - Registra warning
	///    - Retorna null al controller
	/// 4. Si existe:
	///    - VALIDACIÓN: Verifica que el nuevo DocumentNumber no esté usado por OTRO cliente
	///    - Si está duplicado: Registra warning y lanza InvalidOperationException
	/// 5. Si todo OK:
	///    - Mapea los campos del DTO a la entidad existente (in-place)
	///    - Llama al repositorio para actualizar
	///    - El repositorio ejecuta UPDATE con auditoría automática (UpdatedBy, UpdatedAt)
	/// 6. Registra log de éxito
	/// 7. Mapea Client → ClientDto
	/// 8. Retorna ClientDto al controller
	/// 
	/// VALIDACIÓN DE NEGOCIO - DOCUMENTO ÚNICO:
	/// ------------------------------------------------------------------------
	/// var clientWithSameDoc = await _repository.GetByDocumentAsync(dto.DocumentNumber);
	/// if (clientWithSameDoc != null && clientWithSameDoc.Id != id)
	///     throw new InvalidOperationException("El número de documento ya está en uso por otro cliente.");
	/// 
	/// Esta validación permite:
	/// - Actualizar cliente 42 con DocumentNumber "12345678" (su propio documento) → OK
	/// - Actualizar cliente 42 con DocumentNumber "99999999" si nadie más lo usa → OK
	/// - Actualizar cliente 42 con DocumentNumber "11111111" si cliente 50 lo tiene → ERROR
	/// 
	/// La condición `clientWithSameDoc.Id != id` es crucial:
	/// - Permite que un cliente mantenga su mismo documento al actualizar otros campos
	/// - Previene que use un documento de OTRO cliente
	/// 
	/// MAPEO IN-PLACE (actualización de entidad existente):
	/// ------------------------------------------------------------------------
	/// _mapper.Map(dto, existing);
	/// 
	/// Este sobrecarga de Map actualiza la entidad existing EN EL LUGAR, sin crear una nueva:
	/// 
	/// Antes:
	/// existing = {
	///     Id = 42,
	///     Name = "Acme Corp",
	///     Email = "old@acme.com",
	///     DocumentNumber = "12345678",
	///     CreatedBy = "admin@crm.com",
	///     CreatedAt = 2026-04-01T10:00:00Z
	/// }
	/// 
	/// DTO:
	/// dto = {
	///     Name = "Acme Corporation S.A.",
	///     Email = "new@acme.com",
	///     DocumentNumber = "12345678"
	/// }
	/// 
	/// Después de _mapper.Map(dto, existing):
	/// existing = {
	///     Id = 42,  ← NO cambia
	///     Name = "Acme Corporation S.A.",  ← Actualizado
	///     Email = "new@acme.com",  ← Actualizado
	///     DocumentNumber = "12345678",  ← Sin cambios
	///     CreatedBy = "admin@crm.com",  ← NO cambia (se mantiene)
	///     CreatedAt = 2026-04-01T10:00:00Z  ← NO cambia (se mantiene)
	/// }
	/// 
	/// VENTAJA: Se preservan CreatedBy y CreatedAt originales.
	/// 
	/// AUDITORÍA AUTOMÁTICA (en AppDbContext.SaveChangesAsync):
	/// ------------------------------------------------------------------------
	/// Después del UPDATE, la entidad queda:
	/// {
	///     Id = 42,
	///     Name = "Acme Corporation S.A.",
	///     Email = "new@acme.com",
	///     CreatedBy = "admin@crm.com",  ← Original, sin cambios
	///     CreatedAt = 2026-04-01T10:00:00Z,  ← Original, sin cambios
	///     UpdatedBy = "asesor@crm.com",  ← Asignado del JWT
	///     UpdatedAt = 2026-04-14T15:00:00Z  ← Timestamp actual UTC
	/// }
	/// 
	/// LOGGING ESTRUCTURADO:
	/// ------------------------------------------------------------------------
	/// Inicio:
	/// _logger.LogInformation("Actualizando cliente. ID: {ClientId}", id);
	/// 
	/// No encontrado:
	/// _logger.LogWarning("Cliente no encontrado para actualización. ID: {ClientId}", id);
	/// 
	/// Documento duplicado:
	/// _logger.LogWarning("Intento de actualizar con documento duplicado. ID: {ClientId}, Documento: {DocumentNumber}", 
	///     id, dto.DocumentNumber);
	/// 
	/// Éxito:
	/// _logger.LogInformation("Cliente actualizado exitosamente. ID: {ClientId}", id);
	/// 
	/// MANEJO EN CONTROLLER:
	/// ------------------------------------------------------------------------
	/// [HttpPut("{id}")]
	/// public async Task<ActionResult<ApiResponse>> Update(int id, CreateClientDto dto)
	/// {
	///     try
	///     {
	///         var updated = await _clientService.UpdateAsync(id, dto);
	///         
	///         if (updated == null)
	///             return NotFound(new ApiResponse 
	///             {
	///                 Success = false,
	///                 Message = $"Cliente con ID {id} no encontrado"
	///             });
	///         
	///         return Ok(new ApiResponse 
	///         {
	///             Success = true,
	///             Data = updated
	///         });
	///     }
	///     catch (InvalidOperationException ex)
	///     {
	///         return Conflict(new ApiResponse 
	///         {
	///             Success = false,
	///             Message = ex.Message
	///         });
	///     }
	/// }
	/// 
	/// RESPUESTAS HTTP:
	/// ------------------------------------------------------------------------
	/// Éxito:
	/// PUT /api/clients/42
	/// Body: { "name": "Acme Corp Updated", "documentNumber": "12345678", ... }
	/// 
	/// 200 OK
	/// {
	///     "success": true,
	///     "data": {
	///         "id": 42,
	///         "name": "Acme Corp Updated",
	///         "documentNumber": "12345678",
	///         "createdBy": "admin@crm.com",
	///         "createdAt": "2026-04-01T10:00:00Z",
	///         "updatedBy": "asesor@crm.com",
	///         "updatedAt": "2026-04-14T15:00:00Z"
	///     }
	/// }
	/// 
	/// No encontrado:
	/// PUT /api/clients/999
	/// 
	/// 404 Not Found
	/// {
	///     "success": false,
	///     "message": "Cliente con ID 999 no encontrado"
	/// }
	/// 
	/// Documento duplicado:
	/// PUT /api/clients/42
	/// Body: { "documentNumber": "11111111", ... }  ← Ya usado por cliente 50
	/// 
	/// 409 Conflict
	/// {
	///     "success": false,
	///     "message": "El número de documento ya está en uso por otro cliente."
	/// }
	/// 
	/// CONSIDERACIONES DE CONCURRENCIA:
	/// ------------------------------------------------------------------------
	/// Si dos usuarios actualizan el mismo cliente simultáneamente:
	/// - EF Core usa "Last Write Wins" por defecto
	/// - El último UPDATE sobrescribe el anterior
	/// 
	/// Para prevenir esto, se podría implementar:
	/// - Optimistic Concurrency: Agregar campo RowVersion en Client
	/// - Pessimistic Locking: Usar transacciones con SERIALIZABLE
	/// 
	/// En este CRM simple, Last Write Wins es aceptable.
	/// </remarks>
	public async Task<ClientDto?> UpdateAsync(int id, CreateClientDto dto)
	{
		_logger.LogInformation("Actualizando cliente. ID: {ClientId}", id);
		
		var existing = await _repository.GetByIdAsync(id);
		if (existing == null)
		{
			_logger.LogWarning("Cliente no encontrado para actualización. ID: {ClientId}", id);
			return null;
		}

		// Validar que el documento no esté usado por otro cliente
		var clientWithSameDoc = await _repository.GetByDocumentAsync(dto.DocumentNumber);
		if (clientWithSameDoc != null && clientWithSameDoc.Id != id)
		{
			_logger.LogWarning("Intento de actualizar con documento duplicado. ID: {ClientId}, Documento: {DocumentNumber}", 
				id, dto.DocumentNumber);
			throw new InvalidOperationException("El número de documento ya está en uso por otro cliente.");
		}

		_mapper.Map(dto, existing);
		var updated = await _repository.UpdateAsync(existing);

		_logger.LogInformation("Cliente actualizado exitosamente. ID: {ClientId}", id);

		return _mapper.Map<ClientDto>(updated);
	}

	/// <summary>
	/// Elimina lógicamente un cliente (soft delete).
	/// </summary>
	/// <param name="id">ID del cliente a eliminar</param>
	/// <returns>true si se eliminó exitosamente, false si no se encontró</returns>
	/// <remarks>
	/// PROCESO PASO A PASO:
	/// ------------------------------------------------------------------------
	/// 1. Registra log con el ID del cliente a eliminar
	/// 2. Llama al repositorio para eliminar el cliente
	/// 3. El repositorio:
	///    - Busca el cliente por ID
	///    - Si NO existe o ya está eliminado: retorna false
	///    - Si existe: marca IsDeleted = true (soft delete)
	///    - AppDbContext asigna DeletedBy y DeletedAt automáticamente
	///    - Ejecuta UPDATE (NO DELETE físico)
	/// 4. Si result = true:
	///    - Registra log de éxito
	/// 5. Si result = false:
	///    - Registra warning (no encontrado)
	/// 6. Retorna result al controller
	/// 
	/// SOFT DELETE vs HARD DELETE:
	/// ------------------------------------------------------------------------
	/// SOFT DELETE (implementado aquí):
	/// - Se marca IsDeleted = true
	/// - El registro permanece en la base de datos
	/// - Los filtros globales previenen que aparezca en queries
	/// - Se puede "recuperar" el cliente si fue eliminado por error
	/// 
	/// SQL generado:
	/// UPDATE Clients
	/// SET IsDeleted = 1, DeletedBy = 'admin@crm.com', DeletedAt = '2026-04-14T16:00:00Z'
	/// WHERE Id = 42
	/// 
	/// HARD DELETE (NO implementado):
	/// - Se elimina físicamente el registro
	/// - Datos irrecuperables
	/// - Puede violar integridad referencial si hay registros relacionados
	/// 
	/// SQL:
	/// DELETE FROM Clients WHERE Id = 42
	/// 
	/// VENTAJAS DEL SOFT DELETE:
	/// ------------------------------------------------------------------------
	/// 1. AUDITORÍA Y CUMPLIMIENTO:
	///    - Regulaciones como GDPR requieren historial de eliminaciones
	///    - Se puede rastrear quién eliminó qué y cuándo
	///    - Esencial para auditorías financieras y de compliance
	/// 
	/// 2. RECUPERACIÓN:
	///    - Se puede "deshacer" una eliminación accidental
	///    - Cambiar IsDeleted = false para restaurar el cliente
	///    - Evita pérdida de datos por errores humanos
	/// 
	/// 3. INTEGRIDAD REFERENCIAL:
	///    - Contactos, notas y oportunidades del cliente se mantienen
	///    - El historial completo permanece intacto
	///    - No se rompen relaciones Foreign Key
	/// 
	/// 4. ANÁLISIS HISTÓRICO:
	///    - Se pueden generar reportes de clientes eliminados
	///    - Analizar patrones: ¿por qué se pierden clientes?
	///    - KPIs: tasa de churn (rotación de clientes)
	/// 
	/// AUDITORÍA AUTOMÁTICA (en AppDbContext.SaveChangesAsync):
	/// ------------------------------------------------------------------------
	/// Cuando se llama _repository.DeleteAsync(42):
	/// 
	/// 1. El repositorio marca la entidad para eliminación:
	///    _context.Clients.Remove(client);
	/// 
	/// 2. AppDbContext.SaveChangesAsync() intercepta:
	///    - Detecta estado EntityState.Deleted
	///    - Cambia el estado a EntityState.Modified
	///    - Asigna IsDeleted = true
	///    - Asigna DeletedBy = "admin@crm.com" (del JWT)
	///    - Asigna DeletedAt = DateTime.UtcNow
	/// 
	/// 3. EF Core genera UPDATE en lugar de DELETE:
	/// UPDATE Clients
	/// SET IsDeleted = 1, 
	///     DeletedBy = 'admin@crm.com',
	///     DeletedAt = '2026-04-14T16:00:00Z'
	/// WHERE Id = 42
	/// 
	/// Resultado:
	/// {
	///     Id = 42,
	///     Name = "Acme Corp",
	///     DocumentNumber = "12345678",
	///     IsDeleted = true,  ← Marcado como eliminado
	///     DeletedBy = "admin@crm.com",  ← Quién lo eliminó
	///     DeletedAt = 2026-04-14T16:00:00Z  ← Cuándo lo eliminó
	/// }
	/// 
	/// FILTRO GLOBAL - COMPORTAMIENTO POSTERIOR:
	/// ------------------------------------------------------------------------
	/// Después de eliminar el cliente 42, todas las queries lo excluyen automáticamente:
	/// 
	/// await _context.Clients.ToListAsync()
	/// // SQL: SELECT * FROM Clients WHERE IsDeleted = 0
	/// // Cliente 42 NO aparece
	/// 
	/// await _context.Clients.FindAsync(42)
	/// // Retorna null (filtro global aplicado)
	/// 
	/// LOGGING:
	/// ------------------------------------------------------------------------
	/// Inicio:
	/// _logger.LogInformation("Eliminando cliente. ID: {ClientId}", id);
	/// 
	/// Éxito:
	/// _logger.LogInformation("Cliente eliminado exitosamente. ID: {ClientId}", id);
	/// 
	/// No encontrado:
	/// _logger.LogWarning("Cliente no encontrado para eliminación. ID: {ClientId}", id);
	/// 
	/// MANEJO EN CONTROLLER:
	/// ------------------------------------------------------------------------
	/// [HttpDelete("{id}")]
	/// [Authorize(Roles = "Admin")]  // Solo Admin puede eliminar
	/// public async Task<ActionResult<ApiResponse>> Delete(int id)
	/// {
	///     var deleted = await _clientService.DeleteAsync(id);
	///     
	///     if (!deleted)
	///         return NotFound(new ApiResponse 
	///         {
	///             Success = false,
	///             Message = $"Cliente con ID {id} no encontrado"
	///         });
	///     
	///     return Ok(new ApiResponse 
	///     {
	///         Success = true,
	///         Message = "Cliente eliminado exitosamente"
	///     });
	/// }
	/// 
	/// RESPUESTAS HTTP:
	/// ------------------------------------------------------------------------
	/// Éxito:
	/// DELETE /api/clients/42
	/// 
	/// 200 OK
	/// {
	///     "success": true,
	///     "message": "Cliente eliminado exitosamente"
	/// }
	/// 
	/// No encontrado:
	/// DELETE /api/clients/999
	/// 
	/// 404 Not Found
	/// {
	///     "success": false,
	///     "message": "Cliente con ID 999 no encontrado"
	/// }
	/// 
	/// REGISTROS RELACIONADOS:
	/// ------------------------------------------------------------------------
	/// ¿Qué pasa con contactos, notas y oportunidades del cliente eliminado?
	/// 
	/// - Se MANTIENEN en la base de datos (no se eliminan en cascada)
	/// - También tienen IsDeleted = false (no se marcan como eliminados)
	/// - PERO el filtro global del cliente previene acceso a él
	/// 
	/// Esto permite:
	/// - Mantener historial completo de interacciones
	/// - Analizar datos históricos de clientes perdidos
	/// - Cumplir con regulaciones de retención de datos
	/// 
	/// Si se desea eliminar en cascada, se debería:
	/// 1. Eliminar contactos del cliente
	/// 2. Eliminar notas del cliente
	/// 3. Eliminar oportunidades del cliente
	/// 4. Finalmente eliminar el cliente
	/// 
	/// RECUPERACIÓN DE CLIENTE ELIMINADO:
	/// ------------------------------------------------------------------------
	/// Para "recuperar" un cliente soft-deleted (requiere query especial):
	/// 
	/// // Desactivar filtro global temporalmente
	/// var client = await _context.Clients
	///     .IgnoreQueryFilters()  // ← Incluye eliminados
	///     .FirstOrDefaultAsync(c => c.Id == 42);
	/// 
	/// if (client != null && client.IsDeleted)
	/// {
	///     client.IsDeleted = false;
	///     client.DeletedBy = null;
	///     client.DeletedAt = null;
	///     await _context.SaveChangesAsync();
	///     // Cliente 42 ahora está activo de nuevo
	/// }
	/// </remarks>
	public async Task<bool> DeleteAsync(int id)
	{
		_logger.LogInformation("Eliminando cliente. ID: {ClientId}", id);
		
		var result = await _repository.DeleteAsync(id);
		
		if (result)
			_logger.LogInformation("Cliente eliminado exitosamente. ID: {ClientId}", id);
		else
			_logger.LogWarning("Cliente no encontrado para eliminación. ID: {ClientId}", id);

		return result;
	}
}
