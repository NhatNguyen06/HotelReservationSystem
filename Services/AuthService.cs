using HotelApi.Data;
using HotelApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITokenService _tokens;

    public AuthService(AppDbContext db, IPasswordHasher<User> hasher, ITokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<ServiceResult<UserDto>> RegisterAsync(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLower();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            return ServiceResult<UserDto>.Fail("Email đã được sử dụng. Hãy dùng email khác hoặc đăng nhập.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = dto.Phone,
            Role = UserRole.Customer     // đăng ký công khai chỉ tạo được Customer
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);   // KHÔNG lưu mật khẩu thô

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(UserDto.From(user));
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Cùng một thông báo cho "sai email" và "sai mật khẩu" để không lộ email nào đã đăng ký
        if (user == null)
            return ServiceResult<AuthResponseDto>.Fail("Email hoặc mật khẩu không đúng.");

        if (user.IsLocked)
            return ServiceResult<AuthResponseDto>.Fail("Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");

        if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
            return ServiceResult<AuthResponseDto>.Fail("Bạn đã nhập sai quá nhiều lần. Đăng nhập tạm bị khóa 15 phút.");

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (check == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount > MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockDuration);
                user.FailedLoginCount = 0;
            }
            await _db.SaveChangesAsync();
            return ServiceResult<AuthResponseDto>.Fail("Email hoặc mật khẩu không đúng.");
        }

        // Đăng nhập đúng: xóa bộ đếm lỗi
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        await _db.SaveChangesAsync();

        return ServiceResult<AuthResponseDto>.Ok(new AuthResponseDto
        {
            Token = _tokens.CreateToken(user),
            FullName = user.FullName,
            Role = user.Role.ToString()
        });
    }
}