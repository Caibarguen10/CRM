using System;

namespace CrmService.DTOs;

public class ClientDto
{
	public int Id { get; set; }
	public string DocumentNumber { get; set; } = null!;
	public string FullName { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string Phone { get; set; } = null!;
}
