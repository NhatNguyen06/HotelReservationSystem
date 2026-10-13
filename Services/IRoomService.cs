using HotelApi.Models;

namespace HotelApi.Services;

public interface IRoomService
{
    Task<List<RoomDto>> GetAllAsync();
    Task<RoomDto?> GetByIdAsync(int id);
    Task<RoomDto?> CreateAsync(CreateRoomDto dto);   // null nếu loại phòng không tồn tại hoặc trùng số phòng
    Task<bool> UpdateAsync(int id, UpdateRoomDto dto);
    Task<bool> UpdateStatusAsync(int id, RoomStatus status);
    Task<bool> DeleteAsync(int id);
}