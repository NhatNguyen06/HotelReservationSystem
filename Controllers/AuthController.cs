using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var result = await _auth.RegisterAsync(dto);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return StatusCode(201, new { message = "Đăng ký thành công. Vui lòng đăng nhập." });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        if (!result.Success)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Data);
    }
}