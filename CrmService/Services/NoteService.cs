using AutoMapper;
using CrmService.Domain;
using CrmService.DTOs;
using CrmService.Repositories;

namespace CrmService.Services;

/// <summary>
/// Servicio de notas que implementa la lógica de negocio para gestión de notas de clientes.
/// </summary>
/// <remarks>
/// RESPONSABILIDADES:
/// - Validar que el cliente existe antes de crear notas
/// - Mapear DTOs ↔ Entidades con AutoMapper
/// - Logging estructurado de operaciones
/// - Garantizar trazabilidad de comunicaciones
/// 
/// INMUTABILIDAD DE NOTAS:
/// Este servicio solo tiene CreateAsync, NO tiene UpdateAsync ni DeleteAsync
/// visible. Esto es por diseño: las notas son registros históricos inmutables
/// de comunicación con clientes. Razones:
/// 
/// 1. TRAZABILIDAD: El historial no debe modificarse
/// 2. AUDITORÍA: Las notas son evidencia de interacciones
/// 3. CUMPLIMIENTO: Regulaciones pueden requerir historial inmutable
/// 
/// FLUJO TÍPICO:
/// 1. Asesor tiene llamada con cliente
/// 2. Documenta la llamada creando una nota
/// 3. La nota queda registrada con su nombre (CreatedBy) y timestamp
/// 4. El historial es consultable pero no editable
/// </remarks>
public class NoteService : INoteService
{
	private readonly INoteRepository _repository;
	private readonly IMapper _mapper;
	private readonly ILogger<NoteService> _logger;

	public NoteService(INoteRepository repository, IMapper mapper, ILogger<NoteService> logger)
	{
		_repository = repository;
		_mapper = mapper;
		_logger = logger;
	}

	/// <summary>
	/// Crea una nueva nota asociada a un cliente.
	/// </summary>
	/// <returns>ID de la nota creada</returns>
	/// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
	/// <remarks>
	/// PROCESO:
	/// 1. Validar que cliente existe (ClientExistsAsync)
	/// 2. Mapear CreateNoteDto → ClientNote
	/// 3. Persistir con auditoría automática:
	///    - CreatedBy = usuario del JWT
	///    - CreatedAt = DateTime.UtcNow
	/// 4. Retornar solo el ID de la nota creada
	/// 
	/// ¿Por qué retornar solo ID y no el DTO completo?
	/// - Las notas son simples, el ID es suficiente para confirmación
	/// - Si se necesita el contenido completo, se puede consultar después
	/// - Reduce tamaño de respuesta HTTP
	/// 
	/// LOGGING:
	/// - Registra ClientId al iniciar
	/// - Registra noteId al finalizar exitosamente
	/// - Registra warning si cliente no existe
	/// 
	/// EJEMPLO:
	/// var dto = new CreateNoteDto 
	/// { 
	///     Content = "Reunión presencial. Cliente firmó contrato.",
	///     ClientId = 20
	/// };
	/// var noteId = await _noteService.CreateAsync(dto);
	/// // noteId = 156
	/// // La nota queda almacenada con:
	/// // - CreatedBy = "asesor@crm.com"
	/// // - CreatedAt = 2026-04-14T17:00:00Z
	/// </remarks>
	public async Task<int> CreateAsync(CreateNoteDto dto)
	{
		_logger.LogInformation("Creando nota para cliente ID: {ClientId}", dto.ClientId);
		
		var exists = await _repository.ClientExistsAsync(dto.ClientId);
		if (!exists)
		{
			_logger.LogWarning("Cliente ID {ClientId} no existe", dto.ClientId);
			throw new KeyNotFoundException("El cliente no existe.");
		}

		var note = _mapper.Map<ClientNote>(dto);
		var created = await _repository.CreateAsync(note);

		_logger.LogInformation("Nota creada con ID: {NoteId}", created.Id);
		return created.Id;
	}
}
