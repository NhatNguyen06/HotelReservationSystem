using HotelApi.Models;

namespace HotelApi.Services;

public interface IRoomTypeService
{
    Task<List<RoomTypeDto>> GetAllAsync();
}