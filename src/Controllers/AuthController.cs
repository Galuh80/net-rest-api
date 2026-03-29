using Microsoft.AspNetCore.Mvc;
using RestAPI.Constantas;
using RestAPI.Constantas.DTOs.Auth;
using RestAPI.Models;
using RestAPI.Services.Auth;

namespace RestAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
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
        try
        {
            var user = await _service.RegisterAsync(dto);
            var result = new { user.Id, user.Name, user.Email, user.CreatedAt };
            return CreatedAtAction(null, ApiResponse<object>.Ok(result, "User registered successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginDto dto)
    {
        try
        {
            var token = await _service.LoginAsync(dto);
            return Ok(ApiResponse<object>.Ok(new { token }, "Login successful"));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
