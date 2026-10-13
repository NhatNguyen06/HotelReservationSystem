using HotelApi.Models;

namespace HotelApi.Services;

public interface IStaffReservationService
{
    Task<List<StaffReservationDto>> SearchAsync(string? code, string? guest, DateOnly? date, string? status);
    Task<ServiceResult<StaffReservationDto>> GetAsync(int id);
    Task<ServiceResult<StaffReservationDto>> CreateAsync(StaffReservationInputDto dto);
    Task<ServiceResult<StaffReservationDto>> UpdateAsync(int id, StaffReservationInputDto dto);
    Task<ServiceResult<StaffReservationDto>> CancelAsync(int id, string? reason);
    Task<ServiceResult<List<AvailableRoomDto>>> GetAlternativeRoomsAsync(int id);
    Task<ServiceResult<StaffReservationDto>> CheckInAsync(int id, CheckInDto dto);
    Task<ServiceResult<CheckOutPreviewDto>> PreviewCheckOutAsync(int id);
    Task<ServiceResult<InvoiceDto>> CheckOutAsync(int id, CheckOutDto dto);
    Task<ServiceResult<InvoiceDto>> GetInvoiceAsync(int id);
}