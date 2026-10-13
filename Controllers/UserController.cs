using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]   // cả class chỉ dành cho Admin
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        return Ok(await _users.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(CreateUserDto dto)
    {
        var result = await _users.CreateAsync(dto);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return StatusCode(201, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserDto dto)
    {
        var result = await _users.UpdateAsync(id, dto, User.GetUserId());
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    [HttpPatch("{id}/lock")]
    public async Task<ActionResult<UserDto>> SetLock(int id, LockUserDto dto)
    {
        var result = await _users.SetLockAsync(id, dto.IsLocked, User.GetUserId());
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }
}