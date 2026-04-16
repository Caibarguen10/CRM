using System.ComponentModel.DataAnnotations;

namespace CrmService.DTOs;

public class CreateClientDto
{
	[Required(ErrorMessage = "El número de documento es obligatorio")]
	[MaxLength(50, ErrorMessage = "El número de documento no puede exceder 50 caracteres")]
	public string DocumentNumber { get; set; } = null!;

	[Required(ErrorMessage = "El nombre completo es obligatorio")]
	[MaxLength(200, ErrorMessage = "El nombre completo no puede exceder 200 caracteres")]
	public string FullName { get; set; } = null!;

	[Required(ErrorMessage = "El email es obligatorio")]
	[EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
	public string Email { get; set; } = null!;

	[Required(ErrorMessage = "El teléfono es obligatorio")]
	[MaxLength(30, ErrorMessage = "El teléfono no puede exceder 30 caracteres")]
	public string Phone { get; set; } = null!;
}
