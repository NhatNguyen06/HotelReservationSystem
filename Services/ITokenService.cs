using HotelApi.Models;

namespace HotelApi.Services;

public interface ITokenService
{
    string CreateToken(User user);
}