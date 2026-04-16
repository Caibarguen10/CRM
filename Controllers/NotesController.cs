using CrmService.Common;
using CrmService.DTOs;
using CrmService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación para todos los endpoints
public class NotesController : ControllerBase
{
	private readonly INoteService _service;

	public NotesController(INoteService service)
	{
		_service = service;
	}

	/// <summary>
	/// Crea una nueva nota para un cliente.
	/// Política: NoteManagement (Admin, Asesor)
	/// </summary>
	[HttpPost]
	[Authorize(Policy = "NoteManagement")]
	public async Task<IActionResult> Create([FromBody] CreateNoteDto dto)
	{
		var id = await _service.CreateAsync(dto);
		return Ok(ApiResponse<int>.Ok(id, "Nota creada correctamente."));
	}
}
