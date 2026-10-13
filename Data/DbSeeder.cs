using HotelApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Data;

public static class DbSeeder
{
    public static void SeedUsers(AppDbContext db, IPasswordHasher<User> hasher)
    {
        if (db.Users.Any())
            return;

        void Add(string name, string email, string password, UserRole role)
        {
            var user = new User { FullName = name, Email = email, Phone = "0900000000", Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
        }

        Add("Quản trị viên", "admin@hotel.com", "Admin@123", UserRole.Admin);
        Add("Quản lý Hoàng", "manager@hotel.com", "Manager@123", UserRole.Manager);
        Add("Lễ tân Lan", "staff@hotel.com", "Staff@123", UserRole.Staff);
        Add("Khách Minh", "customer@hotel.com", "Customer@123", UserRole.Customer);

        db.SaveChanges();
    }

    // Tạo ~24 booking đã hoàn tất trong 4 tuần qua, có thanh toán và hóa đơn, chỉ để báo cáo có số liệu
    public static void SeedDemoActivity(AppDbContext db)
    {
        if (db.Reservations.Any(r => r.BookingCode.StartsWith("BK-DEMO")))
            return;   // chỉ tạo một lần

        var rooms = db.Rooms.Include(r => r.RoomType).OrderBy(r => r.Id).ToList();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var methods = new[] { PaymentMethod.CreditCard, PaymentMethod.EWallet, PaymentMethod.Cash };

        DateTime Utc(DateOnly d, int hour) => d.ToDateTime(new TimeOnly(hour, 0)).ToUniversalTime();

        for (var i = 0; i < 24; i++)
        {
            var room = rooms[i % rooms.Count];             // mỗi phòng lặp lại sau 8 lần, cách nhau 8 ngày nên không chồng
            var checkIn = today.AddDays(-(4 + i));
            var nights = 1 + i % 3;
            var checkOut = checkIn.AddDays(nights);
            var total = nights * room.RoomType!.BasePrice;
            var cancelled = i % 7 == 6;

            var reservation = new Reservation
            {
                BookingCode = $"BK-DEMO{i:00}",
                GuestName = $"Khách demo {i + 1}",
                GuestPhone = "0900000000",
                RoomId = room.Id,
                CheckIn = checkIn,
                CheckOut = checkOut,
                Guests = 1,
                TotalCost = total,
                Status = cancelled ? ReservationStatus.Cancelled : ReservationStatus.CheckedOut,
                CancelReason = cancelled ? "Khách đổi kế hoạch" : null,
                CheckInTime = cancelled ? null : Utc(checkIn, 14),
                CheckOutTime = cancelled ? null : Utc(checkOut, 11)
            };
            db.Reservations.Add(reservation);

            if (cancelled)
                continue;

            var method = methods[i % 3];
            var extra = i % 5 == 0 ? 100000m : 0m;

            reservation.Payments.Add(new Payment
            {
                Amount = total,
                Method = method,
                Status = PaymentStatus.Paid,
                TransactionCode = $"TX-DEMO{i:00}",
                Message = "Dữ liệu demo",
                CardLast4 = method == PaymentMethod.Cash ? null : "4242",
                CreatedAt = Utc(checkIn.AddDays(-1), 10),
                SessionExpiresAt = Utc(checkIn.AddDays(-1), 10)
            });

            if (extra > 0)
            {
                reservation.Payments.Add(new Payment
                {
                    Amount = extra,
                    Method = PaymentMethod.Cash,
                    Status = PaymentStatus.Paid,
                    TransactionCode = $"TX-DEMOX{i:00}",
                    Message = "Thu khi check-out (demo)",
                    CreatedAt = Utc(checkOut, 11),
                    SessionExpiresAt = Utc(checkOut, 11)
                });
            }

            reservation.Invoice = new Invoice
            {
                InvoiceNumber = $"INV-DEMO{i:00}",
                RoomCharge = total,
                ExtraCharge = extra,
                ExtraChargeNote = extra > 0 ? "Minibar" : null,
                Total = total + extra,
                PaidBefore = total,
                PaidAtCheckout = extra,
                IssuedAt = Utc(checkOut, 11)
            };
        }

        db.SaveChanges();
    }
}