using HotelApi.Models;

namespace HotelApi.Services;

public interface IAvailabilityService
{
    Task<List<AvailableRoomDto>> SearchAsync(DateOnly checkIn, DateOnly checkOut, int guests);
    Task<AvailabilityResultDto?> CheckRoomAsync(int roomId, DateOnly checkIn, DateOnly checkOut);   // null nếu phòng không tồn tại
    Task<bool> IsRoomAvailableAsync(int roomId, DateOnly checkIn, DateOnly checkOut, int? excludeReservationId = null);
}