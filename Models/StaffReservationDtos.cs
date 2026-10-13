using System.ComponentModel.DataAnnotations;

namespace HotelApi.Models;

public class StaffReservationInputDto
{
    [Required(ErrorMessage = "Họ tên khách là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
    public string GuestName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0")]
    public string GuestPhone { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng")]
    public int RoomId { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    [Range(1, 10, ErrorMessage = "Số khách phải từ 1 đến 10")]
    public int Guests { get; set; }
}

public class CheckInDto
{
    public bool IdentityVerified { get; set; }   // đã đối chiếu giấy tờ tùy thân
    public int? RoomId { get; set; }             // null = giữ phòng đã gán
}

public class CheckOutDto
{
    [Range(0, 100000000, ErrorMessage = "Phí phát sinh không hợp lệ")]
    public decimal ExtraCharge { get; set; }

    [StringLength(200, ErrorMessage = "Ghi chú tối đa 200 ký tự")]
    public string? ExtraChargeNote { get; set; }

    public bool CollectBalance { get; set; }                       // đã thu phần còn thiếu
    public PaymentMethod BalanceMethod { get; set; } = PaymentMethod.Cash;
}

public class StaffReservationDto
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }
    public int Guests { get; set; }
    public decimal TotalCost { get; set; }
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public bool CanCheckIn { get; set; }
    public bool CanCheckOut { get; set; }
    public bool CanEdit { get; set; }
    public bool CanCancel { get; set; }
    public bool HasInvoice { get; set; }
}

public class CheckOutPreviewDto
{
    public decimal RoomCharge { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsLate { get; set; }
    public decimal SuggestedLateSurcharge { get; set; }
    public string StandardCheckOutTime { get; set; } = string.Empty;
}

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string BookingCode { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }
    public decimal RoomCharge { get; set; }
    public decimal ExtraCharge { get; set; }
    public string? ExtraChargeNote { get; set; }
    public decimal Total { get; set; }
    public decimal PaidBefore { get; set; }
    public decimal PaidAtCheckout { get; set; }
    public decimal RefundDue { get; set; }
    public DateTime IssuedAt { get; set; }
}