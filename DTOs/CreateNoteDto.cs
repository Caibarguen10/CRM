using System.ComponentModel.DataAnnotations;

namespace CrmService.DTOs;

public class CreateNoteDto
{
	[Required(ErrorMessage = "El ID del cliente es obligatorio")]
	public int ClientId { get; set; }

	[Required(ErrorMessage = "La nota es obligatoria")]
	[MaxLength(1000, ErrorMessage = "La nota no puede exceder 1000 caracteres")]
	public string Note { get; set; } = null!;
}
