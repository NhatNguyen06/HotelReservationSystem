using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class RoomTypeService : IRoomTypeService
{
    private readonly AppDbContext _db;

    public RoomTypeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<RoomTypeDto>> GetAllAsync()
    {
        var types = await _db.RoomTypes.ToListAsync();
        return types.Select(t => new RoomTypeDto
        {
            Id = t.Id,
            Name = t.Name,
            BasePrice = t.BasePrice,
            Capacity = t.Capacity,
            Amenities = t.Amenities
        }).ToList();
    }
}