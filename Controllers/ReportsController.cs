using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Manager)]   // chỉ Manager (Staff không xem được báo cáo)
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    private ActionResult<T> ToResult<T>(ServiceResult<T> result)
    {
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // GET /api/reports/bookings?from=2026-10-01&to=2026-10-31
    [HttpGet("bookings")]
    public async Task<ActionResult<BookingReportDto>> Bookings(
        [FromQuery(Name = "from")] DateOnly? fromDate, [FromQuery(Name = "to")] DateOnly? toDate)
        => ToResult(await _reports.BookingsAsync(fromDate, toDate));

    [HttpGet("rooms")]
    public async Task<ActionResult<RoomReportDto>> Rooms(
        [FromQuery(Name = "from")] DateOnly? fromDate, [FromQuery(Name = "to")] DateOnly? toDate)
        => ToResult(await _reports.RoomsAsync(fromDate, toDate));

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportDto>> Revenue(
        [FromQuery(Name = "from")] DateOnly? fromDate, [FromQuery(Name = "to")] DateOnly? toDate)
        => ToResult(await _reports.RevenueAsync(fromDate, toDate));
}