using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Customer)]   // chỉ khách hàng đã đăng nhập
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    // POST /api/reservations
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationDto dto)
    {
        var result = await _reservations.CreateAsync(User.GetUserId(), dto);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    // GET /api/reservations/my
    [HttpGet("my")]
    public async Task<ActionResult<List<ReservationDto>>> GetMine()
    {
        return Ok(await _reservations.GetMineAsync(User.GetUserId()));
    }

    // GET /api/reservations/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetById(int id)
    {
        var result = await _reservations.GetMineByIdAsync(User.GetUserId(), id);
        if (result.NotFound)
            return NotFound();

        return Ok(result.Data);
    }

    // POST /api/reservations/5/cancel
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ReservationDto>> Cancel(int id, CancelReservationDto dto)
    {
        var result = await _reservations.CancelAsync(User.GetUserId(), id, dto.Reason);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // PUT /api/reservations/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ReservationDto>> Modify(int id, ModifyReservationDto dto)
    {
        var result = await _reservations.ModifyAsync(User.GetUserId(), id, dto);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }


}