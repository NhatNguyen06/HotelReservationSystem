using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class ReportService : IReportService
{
    private const int MaxRangeDays = 92;

    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    // ---------- Hàm hỗ trợ ----------

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    // Mặc định: 30 ngày gần nhất. Trả về lỗi nếu khoảng không hợp lệ
    private static (DateOnly From, DateOnly To, string? Error) ResolveRange(DateOnly? fromDate, DateOnly? toDate)
    {
        var to = toDate ?? Today;
        var from = fromDate ?? to.AddDays(-29);

        if (from > to)
            return (from, to, "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");

        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            return (from, to, $"Khoảng thời gian tối đa là {MaxRangeDays} ngày.");

        return (from, to, null);
    }

    // Đầu ngày (giờ máy chủ) đổi sang UTC để so với các cột DateTime lưu giờ UTC
    private static DateTime ToUtcStart(DateOnly d)
        => TimeZoneInfo.ConvertTimeToUtc(d.ToDateTime(TimeOnly.MinValue), TimeZoneInfo.Local);

    private static IEnumerable<DateOnly> Days(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }

    private static double Percent(int part, int whole)
        => whole == 0 ? 0 : Math.Round(100.0 * part / whole, 1);

    // ---------- Báo cáo booking ----------

    public async Task<ServiceResult<BookingReportDto>> BookingsAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        var (from, to, error) = ResolveRange(fromDate, toDate);
        if (error != null)
            return ServiceResult<BookingReportDto>.Fail(error);

        var list = await _db.Reservations
            .Include(r => r.Room).ThenInclude(room => room!.RoomType)
            .Where(r => r.CheckIn >= from && r.CheckIn <= to)
            .ToListAsync();

        var cancelled = list.Count(r => r.Status == ReservationStatus.Cancelled);

        return ServiceResult<BookingReportDto>.Ok(new BookingReportDto
        {
            From = from,
            To = to,
            TotalBookings = list.Count,
            Pending = list.Count(r => r.Status == ReservationStatus.Pending),
            Confirmed = list.Count(r => r.Status == ReservationStatus.Confirmed),
            CheckedIn = list.Count(r => r.Status == ReservationStatus.CheckedIn),
            CheckedOut = list.Count(r => r.Status == ReservationStatus.CheckedOut),
            Cancelled = cancelled,
            CancellationRate = Percent(cancelled, list.Count),
            OnlineBookings = list.Count(r => r.UserId != null),
            CounterBookings = list.Count(r => r.UserId == null),

            // GroupBy trong bộ nhớ: dữ liệu đã được lọc nhỏ và nhóm theo tên cho dễ đọc
            ByRoomType = list
                .GroupBy(r => r.Room!.RoomType!.Name)
                .Select(g => new ReportItemDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),

            // Đủ mọi ngày trong khoảng, ngày không có booking thì là 0
            ByDay = Days(from, to)
                .Select(d => new ReportItemDto { Label = d.ToString("dd/MM"), Count = list.Count(r => r.CheckIn == d) })
                .ToList()
        });
    }

    // ---------- Báo cáo tình trạng phòng ----------

    public async Task<ServiceResult<RoomReportDto>> RoomsAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        var (from, to, error) = ResolveRange(fromDate, toDate);
        if (error != null)
            return ServiceResult<RoomReportDto>.Fail(error);

        var today = Today;
        var rooms = await _db.Rooms.Include(r => r.RoomType).ToListAsync();
        var total = rooms.Count;

        var arrivals = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.Confirmed && r.CheckIn == today);
        var departures = await _db.Reservations
            .CountAsync(r => r.Status == ReservationStatus.CheckedIn && r.CheckOut <= today);

        // Mọi booking có chiếm đêm nào đó trong khoảng
        var busy = await _db.Reservations
            .Where(r => r.CheckIn <= to && r.CheckOut > from &&
                        (r.Status == ReservationStatus.Confirmed ||
                         r.Status == ReservationStatus.CheckedIn ||
                         r.Status == ReservationStatus.CheckedOut))
            .ToListAsync();

        var occupancy = Days(from, to).Select(d =>
        {
            var n = busy.Count(r => r.CheckIn <= d && r.CheckOut > d);   // booking chiếm đêm d
            return new OccupancyDayDto { Label = d.ToString("dd/MM"), OccupiedRooms = n, Rate = Percent(n, total) };
        }).ToList();

        return ServiceResult<RoomReportDto>.Ok(new RoomReportDto
        {
            Today = today,
            TotalRooms = total,
            Available = rooms.Count(r => r.Status == RoomStatus.Available),
            Occupied = rooms.Count(r => r.Status == RoomStatus.Occupied),
            NeedsCleaning = rooms.Count(r => r.Status == RoomStatus.NeedsCleaning),
            ArrivalsToday = arrivals,
            DeparturesToday = departures,
            ByRoomType = rooms
                .GroupBy(r => r.RoomType!.Name)
                .Select(g => new RoomTypeStatusDto
                {
                    RoomTypeName = g.Key,
                    Total = g.Count(),
                    Available = g.Count(r => r.Status == RoomStatus.Available),
                    Occupied = g.Count(r => r.Status == RoomStatus.Occupied),
                    NeedsCleaning = g.Count(r => r.Status == RoomStatus.NeedsCleaning)
                }).ToList(),
            From = from,
            To = to,
            AverageOccupancy = occupancy.Count == 0 ? 0 : Math.Round(occupancy.Average(o => o.Rate), 1),
            OccupancyByDay = occupancy
        });
    }

    // ---------- Báo cáo doanh thu ----------

    public async Task<ServiceResult<RevenueReportDto>> RevenueAsync(DateOnly? fromDate, DateOnly? toDate)
    {
        var (from, to, error) = ResolveRange(fromDate, toDate);
        if (error != null)
            return ServiceResult<RevenueReportDto>.Fail(error);

        var fromUtc = ToUtcStart(from);
        var toUtc = ToUtcStart(to.AddDays(1));   // hết ngày `to` = đầu ngày kế tiếp

        // Doanh thu = tiền thật sự đã thu (Payment trạng thái Paid)
        var payments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.CreatedAt >= fromUtc && p.CreatedAt < toUtc)
            .ToListAsync();

        var invoiceTotals = await _db.Invoices
            .Where(i => i.IssuedAt >= fromUtc && i.IssuedAt < toUtc)
            .Select(i => i.Total)
            .ToListAsync();

        // Cộng tiền bằng C# vì SQLite lưu decimal dạng chuỗi, Sum trong SQL không chạy được
        var byDay = payments
            .GroupBy(p => DateOnly.FromDateTime(DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc).ToLocalTime()))
            .ToDictionary(g => g.Key, g => (Amount: g.Sum(p => p.Amount), Count: g.Count()));

        return ServiceResult<RevenueReportDto>.Ok(new RevenueReportDto
        {
            From = from,
            To = to,
            TotalRevenue = payments.Sum(p => p.Amount),
            PaymentCount = payments.Count,
            InvoiceCount = invoiceTotals.Count,
            AverageInvoice = invoiceTotals.Count == 0 ? 0 : Math.Round(invoiceTotals.Sum() / invoiceTotals.Count, 0),

            ByMethod = payments
                .GroupBy(p => p.Method.ToString())
                .Select(g => new ReportItemDto { Label = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Amount)
                .ToList(),

            ByDay = Days(from, to)
                .Select(d => new ReportItemDto
                {
                    Label = d.ToString("dd/MM"),
                    Amount = byDay.TryGetValue(d, out var v) ? v.Amount : 0,
                    Count = byDay.TryGetValue(d, out var v2) ? v2.Count : 0
                })
                .ToList()
        });
    }
}