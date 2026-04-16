using AutoMapper;
using CrmService.Domain;
using CrmService.DTOs;
using CrmService.Repositories;

namespace CrmService.Services;

/// <summary>
/// Servicio de oportunidades que implementa la lógica de negocio para gestión de oportunidades de venta.
/// </summary>
/// <remarks>
/// RESPONSABILIDADES:
/// - Validar que el cliente existe antes de crear oportunidades
/// - Mapear DTOs ↔ Entidades con AutoMapper
/// - Logging estructurado de operaciones
/// - Garantizar integridad del pipeline de ventas
/// 
/// INMUTABILIDAD DE OPORTUNIDADES:
/// Este servicio solo tiene CreateAsync, NO tiene UpdateAsync ni DeleteAsync.
/// Razones de diseño:
/// 
/// 1. PIPELINE HISTÓRICO: El pipeline debe ser inmutable para análisis
/// 2. FORECASTING: Reportes se basan en datos históricos inalterables
/// 3. KPIs: Métricas de conversión requieren datos consistentes
/// 4. AUDITORÍA: Evidencia de gestión comercial
/// 
/// Si una oportunidad cambia de estado, se crea un nuevo registro en lugar
/// de modificar el existente. Esto mantiene historial completo.
/// 
/// SALES PIPELINE:
/// El conjunto de oportunidades forma el "pipeline de ventas":
/// - Visualización de oportunidades activas por estado
/// - Suma de montos por estado (Amount)
/// - Forecasting de ingresos futuros
/// - Análisis de cuellos de botella en el proceso
/// </remarks>
public class OpportunityService : IOpportunityService
{
	private readonly IOpportunityRepository _repository;
	private readonly IMapper _mapper;
	private readonly ILogger<OpportunityService> _logger;

	public OpportunityService(IOpportunityRepository repository, IMapper mapper, ILogger<OpportunityService> logger)
	{
		_repository = repository;
		_mapper = mapper;
		_logger = logger;
	}

	/// <summary>
	/// Crea una nueva oportunidad de venta para un cliente.
	/// </summary>
	/// <returns>ID de la oportunidad creada</returns>
	/// <exception cref="KeyNotFoundException">Si el cliente no existe</exception>
	/// <remarks>
	/// PROCESO:
	/// 1. Validar que cliente existe (ClientExistsAsync)
	/// 2. Mapear CreateOpportunityDto → Opportunity
	/// 3. Persistir con auditoría automática:
	///    - CreatedBy = vendedor del JWT
	///    - CreatedAt = DateTime.UtcNow
	/// 4. Retornar solo el ID de la oportunidad
	/// 
	/// DECIMAL PRECISION:
	/// El campo Amount debe usar decimal, NO float/double:
	/// 
	/// float x = 0.1f + 0.2f;
	/// // Resultado: 0.30000001 (ERROR de redondeo)
	/// 
	/// decimal x = 0.1m + 0.2m;
	/// // Resultado: 0.3 (EXACTO)
	/// 
	/// Esto es crítico en valores monetarios para evitar
	/// discrepancias en facturación y reportes.
	/// 
	/// LOGGING:
	/// - Registra ClientId al iniciar
	/// - Registra OpportunityId al finalizar
	/// - Registra warning si cliente no existe
	/// 
	/// MÉTRICAS POSTERIORES:
	/// Con las oportunidades creadas se pueden calcular:
	/// - Pipeline total: SUM(Amount) WHERE Status != 'ClosedLost'
	/// - Tasa de conversión: COUNT(ClosedWon) / COUNT(*)
	/// - Valor promedio: AVG(Amount) WHERE ClosedWon
	/// 
	/// EJEMPLO:
	/// var dto = new CreateOpportunityDto 
	/// { 
	///     Title = "Renovación Anual Cliente XYZ",
	///     Amount = 45000.00m,
	///     Status = OpportunityStatus.Qualified,
	///     ClientId = 28
	/// };
	/// var oppId = await _opportunityService.CreateAsync(dto);
	/// // oppId = 312
	/// // La oportunidad queda almacenada con:
	/// // - CreatedBy = "vendedor@crm.com"
	/// // - CreatedAt = 2026-04-14T18:00:00Z
	/// // - Amount = 45000.00 (precisión exacta)
	/// </remarks>
	public async Task<int> CreateAsync(CreateOpportunityDto dto)
	{
		_logger.LogInformation("Creando oportunidad para cliente ID: {ClientId}", dto.ClientId);
		
		var exists = await _repository.ClientExistsAsync(dto.ClientId);
		if (!exists)
		{
			_logger.LogWarning("Cliente ID {ClientId} no existe", dto.ClientId);
			throw new KeyNotFoundException("El cliente no existe.");
		}

		var opportunity = _mapper.Map<Opportunity>(dto);
		var created = await _repository.CreateAsync(opportunity);

		_logger.LogInformation("Oportunidad creada con ID: {OpportunityId}", created.Id);
		return created.Id;
	}
}
