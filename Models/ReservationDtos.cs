using System.ComponentModel.DataAnnotations;

namespace HotelApi.Models;

public class CreateReservationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phòng")]
    public int RoomId { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    [Range(1, 10, ErrorMessage = "Số khách phải từ 1 đến 10")]
    public int Guests { get; set; }
    public bool HasPendingPayment { get; set; }
}

public class ModifyReservationDto
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    [Range(1, 10, ErrorMessage = "Số khách phải từ 1 đến 10")]
    public int Guests { get; set; }
}

public class CancelReservationDto
{
    [StringLength(200, ErrorMessage = "Lý do tối đa 200 ký tự")]
    public string? Reason { get; set; }   // không bắt buộc (UC-009 A2)
}

public class ReservationDto
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public int RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }
    public int Guests { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? HoldExpiresAt { get; set; }
    public bool CanCancel { get; set; }
    public bool CanModify { get; set; }
    public bool HasPendingPayment { get; set; }
}