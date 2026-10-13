namespace HotelApi.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public decimal RoomCharge { get; set; }         // tiền phòng
    public decimal ExtraCharge { get; set; }        // phí phát sinh (phụ thu, hư hỏng...)
    public string? ExtraChargeNote { get; set; }
    public decimal Total { get; set; }

    public decimal PaidBefore { get; set; }         // đã trả trước (thanh toán online)
    public decimal PaidAtCheckout { get; set; }     // thu thêm khi check-out

    public DateTime IssuedAt { get; set; }         // giờ UTC
}