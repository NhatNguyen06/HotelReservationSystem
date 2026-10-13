using HotelApi.Models;

namespace HotelApi.Services;

public interface IReportService
{
    Task<ServiceResult<BookingReportDto>> BookingsAsync(DateOnly? fromDate, DateOnly? toDate);
    Task<ServiceResult<RoomReportDto>> RoomsAsync(DateOnly? fromDate, DateOnly? toDate);
    Task<ServiceResult<RevenueReportDto>> RevenueAsync(DateOnly? fromDate, DateOnly? toDate);
}