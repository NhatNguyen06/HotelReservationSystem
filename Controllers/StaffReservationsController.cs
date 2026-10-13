using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/staff/reservations")]
[Authorize(Roles = Roles.StaffAndManager)]   // Manager kế thừa chức năng của Staff
public class StaffReservationsController : ControllerBase
{
    private readonly IStaffReservationService _service;

    public StaffReservationsController(IStaffReservationService service)
    {
        _service = service;
    }

    // Đổi ServiceResult thành mã HTTP, dùng chung cho nhiều action
    private ActionResult<T> ToResult<T>(ServiceResult<T> result)
    {
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // GET /api/staff/reservations?code=&guest=&date=2026-10-12&status=Confirmed
    [HttpGet]
    public async Task<ActionResult<List<StaffReservationDto>>> Search(
        [FromQuery] string? code, [FromQuery] string? guest, [FromQuery] DateOnly? date, [FromQuery] string? status)
    {
        return Ok(await _service.SearchAsync(code, guest, date, status));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StaffReservationDto>> Get(int id)
        => ToResult(await _service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<StaffReservationDto>> Create(StaffReservationInputDto dto)
    {
        var result = await _service.CreateAsync(dto);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return CreatedAtAction(nameof(Get), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<StaffReservationDto>> Update(int id, StaffReservationInputDto dto)
        => ToResult(await _service.UpdateAsync(id, dto));

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<StaffReservationDto>> Cancel(int id, CancelReservationDto dto)
        => ToResult(await _service.CancelAsync(id, dto.Reason));

    [HttpGet("{id:int}/alternative-rooms")]
    public async Task<ActionResult<List<AvailableRoomDto>>> AlternativeRooms(int id)
        => ToResult(await _service.GetAlternativeRoomsAsync(id));

    [HttpPost("{id:int}/check-in")]
    public async Task<ActionResult<StaffReservationDto>> CheckIn(int id, CheckInDto dto)
        => ToResult(await _service.CheckInAsync(id, dto));

    [HttpGet("{id:int}/checkout-preview")]
    public async Task<ActionResult<CheckOutPreviewDto>> CheckOutPreview(int id)
        => ToResult(await _service.PreviewCheckOutAsync(id));

    [HttpPost("{id:int}/check-out")]
    public async Task<ActionResult<InvoiceDto>> CheckOut(int id, CheckOutDto dto)
        => ToResult(await _service.CheckOutAsync(id, dto));

    [HttpGet("{id:int}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
        => ToResult(await _service.GetInvoiceAsync(id));
}