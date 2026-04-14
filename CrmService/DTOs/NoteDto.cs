namespace CrmService.DTOs;

public class NoteDto
{
	public int Id { get; set; }
	public int ClientId { get; set; }
	public string Note { get; set; } = null!;
	public DateTime CreatedAt { get; set; }
}
