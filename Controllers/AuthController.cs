using CrmService.Common;
using CrmService.DTOs;
using CrmService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly ILogger<AuthController> _logger;

	public AuthController(IAuthService authService, ILogger<AuthController> logger)
	{
		_authService = authService;
		_logger = logger;
	}

	[HttpPost("register")]
	public async Task<IActionResult> Register([FromBody] RegisterDto dto)
	{
		try
		{
			_logger.LogInformation("Intento de registro para usuario: {Username}", dto.Username);
			var response = await _authService.RegisterAsync(dto);
			return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(response, "Usuario registrado exitosamente"));
		}
		catch (InvalidOperationException ex)
		{
			_logger.LogWarning("Registro fallido para usuario {Username}: {Message}", dto.Username, ex.Message);
			return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
		}
	}

	[HttpPost("login")]
	public async Task<IActionResult> Login([FromBody] LoginDto dto)
	{
		try
		{
			_logger.LogInformation("Intento de login para usuario: {Username}", dto.Username);
			var response = await _authService.LoginAsync(dto);
			_logger.LogInformation("Login exitoso para usuario: {Username}", dto.Username);
			return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(response, "Login exitoso"));
		}
		catch (UnauthorizedAccessException ex)
		{
			_logger.LogWarning("Login fallido para usuario {Username}: {Message}", dto.Username, ex.Message);
			return Unauthorized(ApiResponse<object>.ErrorResponse(ex.Message));
		}
	}
}
