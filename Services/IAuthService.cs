using HotelApi.Models;

namespace HotelApi.Services;

public interface IAuthService
{
    Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto dto);
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto);
}