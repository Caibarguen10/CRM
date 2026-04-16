using AutoMapper;
using CrmService.Domain;
using CrmService.DTOs;
using CrmService.Repositories;

namespace CrmService.Services;

/// <summary>
/// Servicio de contactos que implementa la lógica de negocio para gestión de contactos.
/// </summary>
/// <remarks>
/// RESPONSABILIDADES:
/// - Validar que el cliente existe antes de crear contactos
/// - Mapear DTOs ↔ Entidades con AutoMapper
/// - Logging estructurado de operaciones
/// - Coordinar llamadas al repositorio
/// 
/// FLUJO TÍPICO (CreateAsync):
/// 1. Validar que cliente existe (ClientExistsAsync)
/// 2. Mapear DTO → Entity
/// 3. Persistir en BD con auditoría automática
/// 4. Mapear Entity → DTO
/// 5. Retornar DTO al controller
/// </remarks>
public class ContactService : IContactService
{
	private readonly IContactRepository _repository;
	private readonly IMapper _mapper;
	private readonly ILogger<ContactService> _logger;

	public ContactService(IContactRepository repository, IMapper mapper, ILogger<ContactService> logger)
	{
		_repository = repository;
		_mapper = mapper;
		_logger = logger;
	}

	/// <summary>
	/// Obtiene todos los contactos de un cliente específico.
	/// </summary>
	public async Task<List<ContactDto>> GetByClientIdAsync(int clientId)
	{
		_logger.LogInformation("Obteniendo contactos para cliente ID: {ClientId}", clientId);
		var contacts = await _repository.GetByClientIdAsync(clientId);
		_logger.LogInformation("Se obtuvieron {Count} contactos", contacts.Count);
		return _mapper.Map<List<ContactDto>>(contacts);
	}

	/// <summary>
	/// Crea un nuevo contacto para un cliente.
	/// </summary>
	/// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
	/// <remarks>
	/// VALIDACIÓN DE NEGOCIO:
	/// - Verifica que el cliente existe antes de crear el contacto
	/// - Previene contactos huérfanos (sin cliente válido)
	/// 
	/// EJEMPLO:
	/// var dto = new CreateContactDto 
	/// { 
	///     Name = "Juan Pérez",
	///     Email = "juan@empresa.com",
	///     Position = "Gerente General",
	///     ClientId = 10
	/// };
	/// var created = await _contactService.CreateAsync(dto);
	/// // created.Id = 25 (asignado por BD)
	/// // created.CreatedBy = "admin@crm.com" (del JWT)
	/// </remarks>
	public async Task<ContactDto> CreateAsync(CreateContactDto dto)
	{
		_logger.LogInformation("Creando contacto para cliente ID: {ClientId}", dto.ClientId);
		
		var exists = await _repository.ClientExistsAsync(dto.ClientId);
		if (!exists)
		{
			_logger.LogWarning("Cliente ID {ClientId} no existe", dto.ClientId);
			throw new KeyNotFoundException("El cliente no existe.");
		}

		var contact = _mapper.Map<Contact>(dto);
		var created = await _repository.CreateAsync(contact);

		_logger.LogInformation("Contacto creado con ID: {ContactId}", created.Id);
		return _mapper.Map<ContactDto>(created);
	}
}
