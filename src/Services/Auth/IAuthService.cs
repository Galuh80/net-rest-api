using RestAPI.Constantas.DTOs.Auth;
using RestAPI.Models;

namespace RestAPI.Services.Auth;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto dto);
    Task<string> LoginAsync(LoginDto dto);
}
