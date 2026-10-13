namespace HotelApi.Services;

// Cổng thanh toán GIẢ LẬP dùng để demo. Khi tích hợp cổng thật, chỉ cần viết class khác
// implement IPaymentGateway và đổi dòng đăng ký trong Program.cs
public class FakePaymentGateway : IPaymentGateway
{
    public async Task<GatewayResult> ChargeAsync(GatewayRequest request)
    {
        await Task.Delay(800);   // giả lập độ trễ mạng tới cổng thanh toán

        if (request.AccountNumber.EndsWith("0002"))
            return new GatewayResult { Outcome = GatewayOutcome.Rejected, Message = "Thẻ/ví không đủ số dư." };

        if (request.AccountNumber.EndsWith("0003"))
            return new GatewayResult { Outcome = GatewayOutcome.Rejected, Message = "Mã OTP không chính xác." };

        if (request.AccountNumber.EndsWith("0004"))
            return new GatewayResult { Outcome = GatewayOutcome.Timeout, Message = "Cổng thanh toán không phản hồi." };

        return new GatewayResult { Outcome = GatewayOutcome.Success, Message = "Thanh toán thành công." };
    }
}