namespace CrmService.DTOs;

public class OpportunityDto
{
	public int Id { get; set; }
	public int ClientId { get; set; }
	public string Title { get; set; } = null!;
	public decimal EstimatedAmount { get; set; }
	public string Status { get; set; } = null!;
	public DateTime CreatedAt { get; set; }
}
