using HotelApi.Models;

namespace HotelApi.Services;

public enum GatewayOutcome
{
    Success,
    Rejected,
    Timeout
}

public class GatewayRequest
{
    public string TransactionCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
}

public class GatewayResult
{
    public GatewayOutcome Outcome { get; set; }
    public string Message { get; set; } = string.Empty;
}

public interface IPaymentGateway
{
    Task<GatewayResult> ChargeAsync(GatewayRequest request);
}