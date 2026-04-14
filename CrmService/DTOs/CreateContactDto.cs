using System.ComponentModel.DataAnnotations;

namespace CrmService.DTOs;

public class CreateContactDto
{
	[Required(ErrorMessage = "El ID del cliente es obligatorio")]
	public int ClientId { get; set; }

	[Required(ErrorMessage = "El nombre es obligatorio")]
	[MaxLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
	public string Name { get; set; } = null!;

	[Required(ErrorMessage = "El cargo/posición es obligatorio")]
	[MaxLength(100, ErrorMessage = "El cargo no puede exceder 100 caracteres")]
	public string Position { get; set; } = null!;

	[Required(ErrorMessage = "El email es obligatorio")]
	[EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
	public string Email { get; set; } = null!;

	[Required(ErrorMessage = "El teléfono es obligatorio")]
	[MaxLength(30, ErrorMessage = "El teléfono no puede exceder 30 caracteres")]
	public string Phone { get; set; } = null!;
}
