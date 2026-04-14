using System.ComponentModel.DataAnnotations;

namespace CrmService.DTOs;

public class CreateOpportunityDto
{
	[Required(ErrorMessage = "El ID del cliente es obligatorio")]
	public int ClientId { get; set; }

	[Required(ErrorMessage = "El título es obligatorio")]
	[MaxLength(200, ErrorMessage = "El título no puede exceder 200 caracteres")]
	public string Title { get; set; } = null!;

	[Required(ErrorMessage = "El monto estimado es obligatorio")]
	[Range(0, double.MaxValue, ErrorMessage = "El monto estimado debe ser mayor o igual a 0")]
	public decimal EstimatedAmount { get; set; }

	[Required(ErrorMessage = "El estado es obligatorio")]
	[MaxLength(50, ErrorMessage = "El estado no puede exceder 50 caracteres")]
	public string Status { get; set; } = null!;
}
