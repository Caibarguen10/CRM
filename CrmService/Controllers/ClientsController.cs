using CrmService.Common;
using CrmService.DTOs;
using CrmService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación para todos los endpoints
public class ClientsController : ControllerBase
{
	private readonly IClientService _service;

	public ClientsController(IClientService service)
	{
		_service = service;
	}

	/// <summary>
	/// Obtiene todos los clientes con paginación y filtros opcionales.
	/// Política: ReadOnly (Admin, Asesor, Auditor)
	/// </summary>
	[HttpGet]
	[Authorize(Policy = "ReadOnly")]
	public async Task<IActionResult> Get(
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10,
		[FromQuery] string? documentNumber = null,
		[FromQuery] string? fullName = null,
		[FromQuery] string? email = null)
	{
		var pagination = new PaginationParams { Page = page, PageSize = pageSize };
		var filters = new ClientFilterDto
		{
			DocumentNumber = documentNumber,
			FullName = fullName,
			Email = email
		};

		var result = await _service.GetAllAsync(pagination, filters);
		return Ok(ApiResponse<PagedResult<ClientDto>>.Ok(result));
	}

	/// <summary>
	/// Obtiene un cliente por su ID.
	/// Política: ReadOnly (Admin, Asesor, Auditor)
	/// </summary>
	[HttpGet("{id}")]
	[Authorize(Policy = "ReadOnly")]
	public async Task<IActionResult> GetById(int id)
	{
		var result = await _service.GetByIdAsync(id);
		if (result == null)
			return NotFound(ApiResponse<string>.Fail("Cliente no encontrado."));
		return Ok(ApiResponse<ClientDto>.Ok(result));
	}

	/// <summary>
	/// Crea un nuevo cliente.
	/// Política: ClientManagement (Admin, Asesor)
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "ClientManagement")]
	public async Task<IActionResult> Create([FromBody] CreateClientDto dto)
	{
		var result = await _service.CreateAsync(dto);
		return Ok(ApiResponse<ClientDto>.Ok(result, "Cliente creado correctamente."));
	}

	/// <summary>
	/// Actualiza un cliente existente.
	/// Política: ClientManagement (Admin, Asesor)
	/// </summary>
	[HttpPut("{id}")]
	[Authorize(Policy = "ClientManagement")]
	public async Task<IActionResult> Update(int id, [FromBody] CreateClientDto dto)
	{
		var result = await _service.UpdateAsync(id, dto);
		if (result == null)
			return NotFound(ApiResponse<string>.Fail("Cliente no encontrado."));
		return Ok(ApiResponse<ClientDto>.Ok(result, "Cliente actualizado correctamente."));
	}

	/// <summary>
	/// Elimina un cliente (soft delete).
	/// Política: DeletePermission (Solo Admin)
	/// </summary>
	[HttpDelete("{id}")]
	[Authorize(Policy = "DeletePermission")]
	public async Task<IActionResult> Delete(int id)
	{
		var result = await _service.DeleteAsync(id);
		if (!result)
			return NotFound(ApiResponse<string>.Fail("Cliente no encontrado."));
		return Ok(ApiResponse<string>.Ok(null!, "Cliente eliminado correctamente."));
	}
}
