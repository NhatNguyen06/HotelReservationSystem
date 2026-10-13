using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/room-types")]
public class RoomTypesController : ControllerBase
{
    private readonly IRoomTypeService _roomTypeService;

    public RoomTypesController(IRoomTypeService roomTypeService)
    {
        _roomTypeService = roomTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoomTypeDto>>> GetAll()
    {
        return Ok(await _roomTypeService.GetAllAsync());
    }
}