using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Auth;
using RestAPI.Models;
using RestAPI.Services.Auth;

namespace RestAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;

    public AuthController(IAuthService service)
    {
        _service = service;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterDto dto)
    {
        var user = await _service.RegisterAsync(dto);
        var result = new { user.Id, user.Name, user.Email, user.CreatedAt };
        return CreatedAtAction(null, ApiResponse<object>.Ok(result, "User registered successfully"));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login([FromBody] LoginDto dto)
    {
        var authResponse = await _service.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.Ok(authResponse, "Login successful"));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Refresh([FromBody] RefreshTokenDto dto)
    {
        var authResponse = await _service.RefreshTokenAsync(dto.RefreshToken);
        return Ok(ApiResponse<AuthResponseDto>.Ok(authResponse, "Token refreshed successfully"));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] RefreshTokenDto dto)
    {
        await _service.LogoutAsync(dto.RefreshToken);
        return Ok(ApiResponse<object>.Ok(null!, "Logged out successfully"));
    }
}
