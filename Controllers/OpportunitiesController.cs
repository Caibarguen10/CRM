using CrmService.Common;
using CrmService.DTOs;
using CrmService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación para todos los endpoints
public class OpportunitiesController : ControllerBase
{
	private readonly IOpportunityService _service;

	public OpportunitiesController(IOpportunityService service)
	{
		_service = service;
	}

	/// <summary>
	/// Crea una nueva oportunidad de negocio.
	/// Política: AdminOrAsesor (Admin, Asesor)
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "AdminOrAsesor")]
	public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto)
	{
		var id = await _service.CreateAsync(dto);
		return Ok(ApiResponse<int>.Ok(id, "Oportunidad creada correctamente."));
	}
}
