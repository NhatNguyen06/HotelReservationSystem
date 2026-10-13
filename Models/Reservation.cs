namespace HotelApi.Models;

public class Reservation
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Guests { get; set; }
    public decimal TotalCost { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public DateTime? HoldExpiresAt { get; set; }   // hạn giữ phòng khi còn Pending (giờ UTC)
    public int? UserId { get; set; }          // null với 2 booking mẫu và booking đặt trực tiếp tại quầy (làm sau)
    public User? User { get; set; }
    public string? CancelReason { get; set; }
    public List<Payment> Payments { get; set; } = new();
    public string? GuestName { get; set; }      // khách vãng lai (không có tài khoản)
    public string? GuestPhone { get; set; }
    public DateTime? CheckInTime { get; set; }  // giờ UTC
    public DateTime? CheckOutTime { get; set; } // giờ UTC
    public Invoice? Invoice { get; set; }
}