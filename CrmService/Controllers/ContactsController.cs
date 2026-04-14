using CrmService.Common;
using CrmService.DTOs;
using CrmService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación para todos los endpoints
public class ContactsController : ControllerBase
{
	private readonly IContactService _service;

	public ContactsController(IContactService service)
	{
		_service = service;
	}

	/// <summary>
	/// Obtiene todos los contactos de un cliente.
	/// Política: ReadOnly (Admin, Asesor, Auditor)
	/// </summary>
	[HttpGet("client/{clientId:int}")]
	[Authorize(Policy = "ReadOnly")]
	public async Task<IActionResult> GetByClient(int clientId)
	{
		var result = await _service.GetByClientIdAsync(clientId);
		return Ok(ApiResponse<List<ContactDto>>.Ok(result));
	}

	/// <summary>
	/// Crea un nuevo contacto.
	/// Política: ClientManagement (Admin, Asesor)
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "ClientManagement")]
	public async Task<IActionResult> Create([FromBody] CreateContactDto dto)
	{
		var result = await _service.CreateAsync(dto);
		return Ok(ApiResponse<ContactDto>.Ok(result, "Contacto creado correctamente."));
	}
}
