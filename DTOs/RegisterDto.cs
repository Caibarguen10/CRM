using System.ComponentModel.DataAnnotations;
using CrmService.Domain;

namespace CrmService.DTOs;

public class RegisterDto
{
	[Required(ErrorMessage = "El nombre de usuario es obligatorio")]
	[MinLength(3, ErrorMessage = "El usuario debe tener al menos 3 caracteres")]
	public string Username { get; set; } = null!;

	[Required(ErrorMessage = "La contraseña es obligatoria")]
	[MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
	public string Password { get; set; } = null!;

	[Required(ErrorMessage = "El email es obligatorio")]
	[EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
	public string Email { get; set; } = null!;

	public UserRole Role { get; set; } = UserRole.Asesor; // Por defecto Asesor
}
