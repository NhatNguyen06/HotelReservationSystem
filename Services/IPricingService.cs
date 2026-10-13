using HotelApi.Models;

namespace HotelApi.Services;

public interface IPricingService
{
    Task<ServiceResult<RoomTypeDto>> UpdatePriceAsync(int roomTypeId, decimal newPrice, string? reason, int userId);
    Task<List<PriceHistoryDto>> GetHistoryAsync(int? roomTypeId);
}