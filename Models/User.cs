namespace HotelApi.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;

    public bool IsLocked { get; set; }            // Admin khóa
    public int FailedLoginCount { get; set; }     // đếm lần nhập sai liên tiếp
    public DateTime? LockoutEnd { get; set; }     // khóa tạm 15 phút khi nhập sai nhiều (giờ UTC)
}