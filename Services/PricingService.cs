using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class PricingService : IPricingService
{
    private readonly AppDbContext _db;

    public PricingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult<RoomTypeDto>> UpdatePriceAsync(int roomTypeId, decimal newPrice, string? reason, int userId)
    {
        var type = await _db.RoomTypes.FindAsync(roomTypeId);
        if (type == null)
            return ServiceResult<RoomTypeDto>.Missing();

        if (newPrice == type.BasePrice)
            return ServiceResult<RoomTypeDto>.Fail("Giá mới trùng với giá hiện tại.");

        _db.PriceHistories.Add(new PriceHistory
        {
            RoomTypeId = type.Id,
            OldPrice = type.BasePrice,
            NewPrice = newPrice,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow
        });

        type.BasePrice = newPrice;

        await _db.SaveChangesAsync();   // đổi giá và ghi nhật ký CÙNG LÚC

        return ServiceResult<RoomTypeDto>.Ok(new RoomTypeDto
        {
            Id = type.Id,
            Name = type.Name,
            BasePrice = type.BasePrice,
            Capacity = type.Capacity,
            Amenities = type.Amenities
        });
    }

    public async Task<List<PriceHistoryDto>> GetHistoryAsync(int? roomTypeId)
    {
        var query = _db.PriceHistories
            .Include(h => h.RoomType)
            .Include(h => h.ChangedBy)
            .AsQueryable();

        if (roomTypeId.HasValue)
            query = query.Where(h => h.RoomTypeId == roomTypeId.Value);

        var list = await query.OrderByDescending(h => h.ChangedAt).Take(50).ToListAsync();

        return list.Select(h => new PriceHistoryDto
        {
            Id = h.Id,
            RoomTypeId = h.RoomTypeId,
            RoomTypeName = h.RoomType?.Name ?? string.Empty,
            OldPrice = h.OldPrice,
            NewPrice = h.NewPrice,
            ChangedByName = h.ChangedBy?.FullName ?? string.Empty,
            Reason = h.Reason,
            ChangedAt = DateTime.SpecifyKind(h.ChangedAt, DateTimeKind.Utc)
        }).ToList();
    }
}