using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class RoomService : IRoomService
{
    private readonly AppDbContext _db;

    public RoomService(AppDbContext db)
    {
        _db = db;
    }

    private static RoomDto ToDto(Room r) => new()
    {
        Id = r.Id,
        RoomNumber = r.RoomNumber,
        Status = r.Status.ToString(),
        RoomTypeId = r.RoomTypeId,
        RoomTypeName = r.RoomType?.Name ?? string.Empty,
        BasePrice = r.RoomType?.BasePrice ?? 0,
        Capacity = r.RoomType?.Capacity ?? 0,
        Amenities = r.RoomType?.Amenities ?? string.Empty
    };

    public async Task<List<RoomDto>> GetAllAsync()
    {
        var rooms = await _db.Rooms
            .Include(r => r.RoomType)
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        return rooms.Select(ToDto).ToList();
    }

    public async Task<RoomDto?> GetByIdAsync(int id)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id);

        return room == null ? null : ToDto(room);
    }

    public async Task<RoomDto?> CreateAsync(CreateRoomDto dto)
    {
        var typeExists = await _db.RoomTypes.AnyAsync(t => t.Id == dto.RoomTypeId);
        var numberExists = await _db.Rooms.AnyAsync(r => r.RoomNumber == dto.RoomNumber);
        if (!typeExists || numberExists)
            return null;

        var room = new Room
        {
            RoomNumber = dto.RoomNumber,
            RoomTypeId = dto.RoomTypeId,
            Status = RoomStatus.Available
        };

        _db.Rooms.Add(room);
        await _db.SaveChangesAsync();
        await _db.Entry(room).Reference(r => r.RoomType).LoadAsync();

        return ToDto(room);
    }

    public async Task<bool> UpdateAsync(int id, UpdateRoomDto dto)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
            return false;

        room.RoomNumber = dto.RoomNumber;
        room.RoomTypeId = dto.RoomTypeId;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, RoomStatus status)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
            return false;

        room.Status = status;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var room = await _db.Rooms.FindAsync(id);
        if (room == null)
            return false;

        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync();
        return true;
    }
}