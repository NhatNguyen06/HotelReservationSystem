using System.ComponentModel.DataAnnotations;

namespace HotelApi.Models;

public class StartPaymentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn booking cần thanh toán")]
    public int ReservationId { get; set; }

    public PaymentMethod Method { get; set; }
}

public class ConfirmPaymentDto
{
    [Required(ErrorMessage = "Vui lòng nhập số thẻ hoặc số ví")]
    [RegularExpression(@"^\d{10,19}$", ErrorMessage = "Số thẻ/ví gồm 10 đến 19 chữ số")]
    public string AccountNumber { get; set; } = string.Empty;
}

public class ReconcilePaymentDto
{
    public bool Success { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TransactionCode { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? CardLast4 { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime SessionExpiresAt { get; set; }
}