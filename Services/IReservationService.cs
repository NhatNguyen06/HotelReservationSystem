using HotelApi.Models;

namespace HotelApi.Services;

public interface IReservationService
{
    Task<ServiceResult<ReservationDto>> CreateAsync(int userId, CreateReservationDto dto);
    Task<List<ReservationDto>> GetMineAsync(int userId);
    Task<ServiceResult<ReservationDto>> GetMineByIdAsync(int userId, int id);
    Task<ServiceResult<ReservationDto>> CancelAsync(int userId, int id, string? reason);
    Task<ServiceResult<ReservationDto>> ModifyAsync(int userId, int id, ModifyReservationDto dto);

    
}