using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.StaffAndManager)]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;
    private readonly IAvailabilityService _availabilityService;
    public RoomsController(IRoomService roomService, IAvailabilityService availabilityService)
    {
        _roomService = roomService;
        _availabilityService = availabilityService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomDto>>> GetAll()
    {
        return Ok(await _roomService.GetAllAsync());
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        if (room == null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<RoomDto>> Create(CreateRoomDto dto)
    {
        var created = await _roomService.CreateAsync(dto);
        if (created == null)
            return BadRequest("Loại phòng không tồn tại hoặc số phòng đã được sử dụng.");

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateRoomDto dto)
    {
        var success = await _roomService.UpdateAsync(id, dto);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateRoomStatusDto dto)
    {
        var success = await _roomService.UpdateStatusAsync(id, dto.Status);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _roomService.DeleteAsync(id);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [AllowAnonymous]                           // mở lại cho mọi người
    [HttpGet("search")]
    public async Task<ActionResult<List<AvailableRoomDto>>> Search(
    [FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut, [FromQuery] int guests = 1)
    {
        var error = ValidateStay(checkIn, checkOut, guests);
        if (error != null)
            return error;

        return Ok(await _availabilityService.SearchAsync(checkIn, checkOut, guests));
    }

    // GET /api/rooms/3/availability?checkIn=2026-10-20&checkOut=2026-10-22
    [AllowAnonymous]
    [HttpGet("{id}/availability")]
    public async Task<ActionResult<AvailabilityResultDto>> CheckAvailability(
        int id, [FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut)
    {
        var error = ValidateStay(checkIn, checkOut, 1);
        if (error != null)
            return error;

        var result = await _availabilityService.CheckRoomAsync(id, checkIn, checkOut);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    private ActionResult? ValidateStay(DateOnly checkIn, DateOnly checkOut, int guests)
    {
        if (checkOut <= checkIn)
            return BadRequest("Ngày trả phòng phải sau ngày nhận phòng.");

        if (checkIn < DateOnly.FromDateTime(DateTime.Today))
            return BadRequest("Ngày nhận phòng không được ở quá khứ.");

        if (guests < 1)
            return BadRequest("Số khách phải từ 1 trở lên.");

        return null;
    }
}