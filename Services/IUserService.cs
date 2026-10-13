using HotelApi.Models;

namespace HotelApi.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto);
    Task<ServiceResult<UserDto>> UpdateAsync(int id, UpdateUserDto dto, int currentUserId);
    Task<ServiceResult<UserDto>> SetLockAsync(int id, bool isLocked, int currentUserId);
    Task<UserDto?> GetProfileAsync(int userId);
    Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
}