namespace CrmService.DTOs;

public class ContactDto
{
	public int Id { get; set; }
	public int ClientId { get; set; }
	public string Name { get; set; } = null!;
	public string Position { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string Phone { get; set; } = null!;
}
