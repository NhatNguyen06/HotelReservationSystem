using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // phải đăng nhập, vai trò nào cũng được
public class ProfileController : ControllerBase
{
    private readonly IUserService _users;

    public ProfileController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<UserDto>> Get()
    {
        var profile = await _users.GetProfileAsync(User.GetUserId());
        if (profile == null)
            return NotFound();

        return Ok(profile);
    }

    [HttpPut]
    public async Task<ActionResult<UserDto>> Update(UpdateProfileDto dto)
    {
        var profile = await _users.UpdateProfileAsync(User.GetUserId(), dto);
        if (profile == null)
            return NotFound();

        return Ok(profile);
    }
}