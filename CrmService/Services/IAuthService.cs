using CrmService.DTOs;

namespace CrmService.Services;

public interface IAuthService
{
	Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
	Task<AuthResponseDto> LoginAsync(LoginDto dto);
}
