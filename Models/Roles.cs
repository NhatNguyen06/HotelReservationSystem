namespace HotelApi.Models;

public static class Roles
{
    public const string Customer = "Customer";
    public const string Admin = "Admin";
    // Manager kế thừa chức năng của Staff, nên mọi chức năng Staff đều cho cả Manager
    public const string StaffAndManager = "Staff,Manager";
    public const string Manager = "Manager";
}