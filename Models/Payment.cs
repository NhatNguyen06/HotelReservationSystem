namespace HotelApi.Models;

public enum PaymentMethod
{
    CreditCard,
    EWallet,
    Cash       // thu tại quầy
}

public enum PaymentStatus
{
    Initiated,   // phiên đang mở
    Pending,     // chưa rõ kết quả (timeout)
    Paid,
    Failed,
    Cancelled
}

public class Payment
{
    public int Id { get; set; }

    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }

    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

    public string TransactionCode { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? CardLast4 { get; set; }            // CHỈ lưu 4 số cuối

    public DateTime CreatedAt { get; set; }            // giờ UTC
    public DateTime SessionExpiresAt { get; set; }     // hạn của phiên thanh toán
}