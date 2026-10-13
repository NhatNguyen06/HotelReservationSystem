using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<RoomType> RoomTypes => Set<RoomType>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Số phòng không được trùng nhau
        modelBuilder.Entity<Room>().HasIndex(r => r.RoomNumber).IsUnique();

        modelBuilder.Entity<RoomType>().HasData(
            new RoomType { Id = 1, Name = "Standard", BasePrice = 500000, Capacity = 2, Amenities = "Wifi, TV, Điều hòa" },
            new RoomType { Id = 2, Name = "Deluxe", BasePrice = 900000, Capacity = 3, Amenities = "Wifi, TV, Điều hòa, Ban công" },
            new RoomType { Id = 3, Name = "Suite", BasePrice = 1800000, Capacity = 4, Amenities = "Wifi, TV, Điều hòa, Bồn tắm, Phòng khách" }
        );

        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, RoomNumber = "101", RoomTypeId = 1, Status = RoomStatus.Available },
            new Room { Id = 2, RoomNumber = "102", RoomTypeId = 1, Status = RoomStatus.Available },
            new Room { Id = 3, RoomNumber = "103", RoomTypeId = 1, Status = RoomStatus.Available },
            new Room { Id = 4, RoomNumber = "201", RoomTypeId = 2, Status = RoomStatus.Available },
            new Room { Id = 5, RoomNumber = "202", RoomTypeId = 2, Status = RoomStatus.Available },
            new Room { Id = 6, RoomNumber = "203", RoomTypeId = 2, Status = RoomStatus.Available },
            new Room { Id = 7, RoomNumber = "301", RoomTypeId = 3, Status = RoomStatus.Available },
            new Room { Id = 8, RoomNumber = "302", RoomTypeId = 3, Status = RoomStatus.Available }
        );

        modelBuilder.Entity<Payment>().Property(p => p.Status).HasConversion<string>();
        modelBuilder.Entity<Payment>().Property(p => p.Method).HasConversion<string>();
        modelBuilder.Entity<Payment>().HasIndex(p => p.TransactionCode).IsUnique();

        modelBuilder.Entity<Reservation>().HasIndex(r => r.BookingCode).IsUnique();

        // Hai booking mẫu để có dữ liệu thử phòng đã bị đặt
        modelBuilder.Entity<Reservation>().HasData(
            new Reservation
            {
                Id = 1,
                BookingCode = "BK-SEED01",
                RoomId = 1,
                CheckIn = new DateOnly(2026, 10, 20),
                CheckOut = new DateOnly(2026, 10, 23),
                Guests = 2,
                TotalCost = 1500000,
                Status = ReservationStatus.Confirmed,
                GuestName = "Khách mẫu A",
                GuestPhone = "0911111111"
            },
            new Reservation
            {
                Id = 2,
                BookingCode = "BK-SEED02",
                RoomId = 4,
                CheckIn = new DateOnly(2026, 10, 21),
                CheckOut = new DateOnly(2026, 10, 24),
                Guests = 3,
                TotalCost = 2700000,
                Status = ReservationStatus.Confirmed,
                GuestName = "Khách mẫu B",
                GuestPhone = "0922222222"
            }
        );
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();   // lưu "Admin" thay vì số 3

        modelBuilder.Entity<Invoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
        modelBuilder.Entity<Invoice>()
        .HasOne(i => i.Reservation)
        .WithOne(r => r.Invoice)
        .HasForeignKey<Invoice>(i => i.ReservationId);   // mỗi booking chỉ có một hóa đơn

        // Tăng tốc truy vấn kiểm tra phòng trống và danh sách booking của khách
        modelBuilder.Entity<Reservation>().HasIndex(r => new { r.RoomId, r.CheckIn, r.CheckOut });
        modelBuilder.Entity<Reservation>().HasIndex(r => r.UserId);
    }
}
