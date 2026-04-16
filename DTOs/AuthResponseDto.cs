namespace CrmService.DTOs;

public class AuthResponseDto
{
	public string Token { get; set; } = null!;
	public string Username { get; set; } = null!;
	public string Email { get; set; } = null!;
	public string Role { get; set; } = null!;
	public DateTime ExpiresAt { get; set; }
}
