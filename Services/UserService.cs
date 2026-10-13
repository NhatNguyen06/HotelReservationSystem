using HotelApi.Data;
using HotelApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public UserService(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    private Task<int> CountActiveAdminsAsync()
        => _db.Users.CountAsync(u => u.Role == UserRole.Admin && !u.IsLocked);

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _db.Users.OrderBy(u => u.Id).ToListAsync();
        return users.Select(UserDto.From).ToList();
    }

    public async Task<ServiceResult<UserDto>> CreateAsync(CreateUserDto dto)
    {
        var email = dto.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return ServiceResult<UserDto>.Fail("Email đã được sử dụng.");

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = dto.Phone,
            Role = dto.Role
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return ServiceResult<UserDto>.Ok(UserDto.From(user));
    }

    public async Task<ServiceResult<UserDto>> UpdateAsync(int id, UpdateUserDto dto, int currentUserId)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ServiceResult<UserDto>.Missing();

        var email = dto.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != id))
            return ServiceResult<UserDto>.Fail("Email đã được sử dụng.");

        if (user.Role != dto.Role)
        {
            if (id == currentUserId)
                return ServiceResult<UserDto>.Fail("Không thể tự đổi vai trò của chính mình.");

            if (user.Role == UserRole.Admin && !user.IsLocked && await CountActiveAdminsAsync() <= 1)
                return ServiceResult<UserDto>.Fail("Không thể đổi vai trò của quản trị viên cuối cùng.");
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.Phone = dto.Phone;
        user.Role = dto.Role;

        await _db.SaveChangesAsync();
        return ServiceResult<UserDto>.Ok(UserDto.From(user));
    }

    public async Task<ServiceResult<UserDto>> SetLockAsync(int id, bool isLocked, int currentUserId)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ServiceResult<UserDto>.Missing();

        if (isLocked)
        {
            if (id == currentUserId)
                return ServiceResult<UserDto>.Fail("Không thể tự khóa tài khoản của chính mình.");

            if (user.Role == UserRole.Admin && !user.IsLocked && await CountActiveAdminsAsync() <= 1)
                return ServiceResult<UserDto>.Fail("Không thể khóa quản trị viên cuối cùng.");
        }

        user.IsLocked = isLocked;
        if (!isLocked)
        {
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
        }

        await _db.SaveChangesAsync();
        return ServiceResult<UserDto>.Ok(UserDto.From(user));
    }

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user == null ? null : UserDto.From(user);
    }

    public async Task<UserDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return null;

        user.FullName = dto.FullName.Trim();
        user.Phone = dto.Phone;

        await _db.SaveChangesAsync();
        return UserDto.From(user);
    }
}