using System.ComponentModel.DataAnnotations;

namespace HotelApi.Models;

// Một dòng dữ liệu dùng chung cho biểu đồ cột: nhãn + số tiền + số lượng
public class ReportItemDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class OccupancyDayDto
{
    public string Label { get; set; } = string.Empty;
    public int OccupiedRooms { get; set; }
    public double Rate { get; set; }   // phần trăm
}

public class BookingReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public int TotalBookings { get; set; }
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int CheckedIn { get; set; }
    public int CheckedOut { get; set; }
    public int Cancelled { get; set; }
    public double CancellationRate { get; set; }
    public int OnlineBookings { get; set; }
    public int CounterBookings { get; set; }
    public List<ReportItemDto> ByRoomType { get; set; } = new();
    public List<ReportItemDto> ByDay { get; set; } = new();
}

public class RoomTypeStatusDto
{
    public string RoomTypeName { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Available { get; set; }
    public int Occupied { get; set; }
    public int NeedsCleaning { get; set; }
}

public class RoomReportDto
{
    public DateOnly Today { get; set; }
    public int TotalRooms { get; set; }
    public int Available { get; set; }
    public int Occupied { get; set; }
    public int NeedsCleaning { get; set; }
    public int ArrivalsToday { get; set; }     // booking đã xác nhận, nhận phòng hôm nay
    public int DeparturesToday { get; set; }   // đang ở, đến hạn (hoặc quá hạn) trả phòng
    public List<RoomTypeStatusDto> ByRoomType { get; set; } = new();

    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public double AverageOccupancy { get; set; }
    public List<OccupancyDayDto> OccupancyByDay { get; set; } = new();
}

public class RevenueReportDto
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public decimal TotalRevenue { get; set; }
    public int PaymentCount { get; set; }
    public int InvoiceCount { get; set; }
    public decimal AverageInvoice { get; set; }
    public List<ReportItemDto> ByMethod { get; set; } = new();
    public List<ReportItemDto> ByDay { get; set; } = new();
}

public class UpdatePriceDto
{
    [Range(1000, 100000000, ErrorMessage = "Giá phải từ 1.000 đến 100.000.000 đ")]
    public decimal NewPrice { get; set; }

    [StringLength(200, ErrorMessage = "Lý do tối đa 200 ký tự")]
    public string? Reason { get; set; }
}

public class PriceHistoryDto
{
    public int Id { get; set; }
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}