using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly AppDbContext _db;

    public AvailabilityService(AppDbContext db)
    {
        _db = db;
    }

    // Quy tắc "booking nào đang chiếm phòng trong khoảng ngày này" chỉ viết MỘT chỗ duy nhất
    private IQueryable<Reservation> OverlappingActive(DateOnly checkIn, DateOnly checkOut)
    {
        var now = DateTime.UtcNow;

        return _db.Reservations.Where(x =>
            x.CheckIn < checkOut && x.CheckOut > checkIn &&
            (x.Status == ReservationStatus.Confirmed ||
             x.Status == ReservationStatus.CheckedIn ||
             (x.Status == ReservationStatus.Pending && x.HoldExpiresAt > now)));
    }

    public async Task<List<AvailableRoomDto>> SearchAsync(DateOnly checkIn, DateOnly checkOut, int guests)
    {
        var busy = OverlappingActive(checkIn, checkOut);

        var rooms = await _db.Rooms
            .Include(r => r.RoomType)
            .Where(r => r.RoomType!.Capacity >= guests)
            .Where(r => !busy.Any(x => x.RoomId == r.Id))   // phòng KHÔNG có booking nào chồng ngày
            .OrderBy(r => r.RoomNumber)
            .ToListAsync();

        var nights = checkOut.DayNumber - checkIn.DayNumber;

        return rooms.Select(r => new AvailableRoomDto
        {
            Id = r.Id,
            RoomNumber = r.RoomNumber,
            Status = r.Status.ToString(),
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType?.Name ?? string.Empty,
            BasePrice = r.RoomType?.BasePrice ?? 0,
            Capacity = r.RoomType?.Capacity ?? 0,
            Amenities = r.RoomType?.Amenities ?? string.Empty,
            Nights = nights,
            TotalPrice = nights * (r.RoomType?.BasePrice ?? 0)
        }).ToList();
    }

    public async Task<AvailabilityResultDto?> CheckRoomAsync(int roomId, DateOnly checkIn, DateOnly checkOut)
    {
        var room = await _db.Rooms
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null)
            return null;

        var nights = checkOut.DayNumber - checkIn.DayNumber;

        return new AvailabilityResultDto
        {
            Available = await IsRoomAvailableAsync(roomId, checkIn, checkOut),
            Nights = nights,
            TotalPrice = nights * (room.RoomType?.BasePrice ?? 0)
        };
    }

    public async Task<bool> IsRoomAvailableAsync(int roomId, DateOnly checkIn, DateOnly checkOut, int? excludeReservationId = null)
    {
        var query = OverlappingActive(checkIn, checkOut).Where(x => x.RoomId == roomId);

        if (excludeReservationId.HasValue)
            query = query.Where(x => x.Id != excludeReservationId.Value);

        return !await query.AnyAsync();
    }
}