using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/pricing")]
[Authorize(Roles = Roles.Manager)]
public class PricingController : ControllerBase
{
    private readonly IPricingService _pricing;

    public PricingController(IPricingService pricing)
    {
        _pricing = pricing;
    }

    // PUT /api/pricing/2   body: { "newPrice": 950000, "reason": "Mùa cao điểm" }
    [HttpPut("{roomTypeId:int}")]
    public async Task<ActionResult<RoomTypeDto>> UpdatePrice(int roomTypeId, UpdatePriceDto dto)
    {
        var result = await _pricing.UpdatePriceAsync(roomTypeId, dto.NewPrice, dto.Reason, User.GetUserId());
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // GET /api/pricing/history?roomTypeId=2
    [HttpGet("history")]
    public async Task<ActionResult<List<PriceHistoryDto>>> History([FromQuery] int? roomTypeId)
    {
        return Ok(await _pricing.GetHistoryAsync(roomTypeId));
    }
}