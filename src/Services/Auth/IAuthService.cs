using RestAPI.Constantas.DTOs.Auth;
using RestAPI.Models;

namespace RestAPI.Services.Auth;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}
