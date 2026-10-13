using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class PaymentService : IPaymentService
{
    private const int SessionMinutes = 5;          // phiên thanh toán sống 5 phút
    private const int TimeoutExtraHoldMinutes = 30; // timeout thì giữ phòng thêm 30 phút

    private readonly AppDbContext _db;
    private readonly IPaymentGateway _gateway;

    public PaymentService(AppDbContext db, IPaymentGateway gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    private static PaymentDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        ReservationId = p.ReservationId,
        BookingCode = p.Reservation?.BookingCode ?? string.Empty,
        CustomerName = p.Reservation?.GuestName ?? p.Reservation?.User?.FullName ?? string.Empty,
        Amount = p.Amount,
        Method = p.Method.ToString(),
        Status = p.Status.ToString(),
        TransactionCode = p.TransactionCode,
        Message = p.Message,
        CardLast4 = p.CardLast4,
        // SpecifyKind để JSON có chữ "Z" (giờ UTC), trình duyệt đổi đúng giờ địa phương
        CreatedAt = DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc),
        SessionExpiresAt = DateTime.SpecifyKind(p.SessionExpiresAt, DateTimeKind.Utc)
    };

    private IQueryable<Payment> WithDetails()
        => _db.Payments
            .Include(p => p.Reservation)
            .ThenInclude(r => r!.User);

    // ---------- UC-007 bước 1: tạo phiên thanh toán ----------

    public async Task<ServiceResult<PaymentDto>> StartAsync(int userId, StartPaymentDto dto)
    {
        var r = await _db.Reservations
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == dto.ReservationId && x.UserId == userId);

        if (r == null)
            return ServiceResult<PaymentDto>.Missing();

        if (r.Status != ReservationStatus.Pending)
            return ServiceResult<PaymentDto>.Fail("Booking này không ở trạng thái chờ thanh toán.");

        if (dto.Method == PaymentMethod.Cash)
            return ServiceResult<PaymentDto>.Fail("Thanh toán tiền mặt chỉ thực hiện tại quầy khách sạn.");

        var now = DateTime.UtcNow;
        if (r.HoldExpiresAt == null || r.HoldExpiresAt <= now)
            return ServiceResult<PaymentDto>.Fail("Đã hết thời gian giữ phòng. Vui lòng đặt lại.");   // UC-006 E2

        // UC-007 E2: đang chờ kết quả giao dịch trước thì không bắt khách trả tiền lại
        if (r.Payments.Any(p => p.Status == PaymentStatus.Pending))
            return ServiceResult<PaymentDto>.Fail(
                "Giao dịch trước đang chờ kết quả từ cổng thanh toán. Bạn không cần thanh toán lại, vui lòng chờ.");

        // Đóng các phiên cũ còn mở (A1: khách đổi phương thức)
        foreach (var old in r.Payments.Where(p => p.Status == PaymentStatus.Initiated))
        {
            old.Status = PaymentStatus.Cancelled;
            old.Message = "Thay bằng phiên thanh toán mới.";
        }

        var payment = new Payment
        {
            ReservationId = r.Id,
            Amount = r.TotalCost,
            Method = dto.Method,
            Status = PaymentStatus.Initiated,
            TransactionCode = "TX-" + Guid.NewGuid().ToString("N")[..10].ToUpper(),
            CreatedAt = now,
            SessionExpiresAt = now.AddMinutes(SessionMinutes)
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        payment.Reservation = r;
        return ServiceResult<PaymentDto>.Ok(ToDto(payment));
    }

    // ---------- UC-007 bước 2: khách nhập thông tin, gọi cổng thanh toán ----------

    public async Task<ServiceResult<PaymentDto>> ConfirmAsync(int userId, int paymentId, string accountNumber)
    {
        var p = await WithDetails()
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.Reservation!.UserId == userId);

        if (p == null)
            return ServiceResult<PaymentDto>.Missing();

        if (p.Status != PaymentStatus.Initiated)
            return ServiceResult<PaymentDto>.Fail("Giao dịch này đã được xử lý hoặc đã bị hủy.");

        var now = DateTime.UtcNow;
        var r = p.Reservation!;

        // E3: phiên thanh toán hết hạn trước khi khách xác nhận
        if (p.SessionExpiresAt <= now)
        {
            p.Status = PaymentStatus.Cancelled;
            p.Message = "Phiên thanh toán đã hết hạn.";
            await _db.SaveChangesAsync();
            return ServiceResult<PaymentDto>.Fail("Phiên thanh toán đã hết hạn. Vui lòng bắt đầu lại.");
        }

        // Hết thời gian giữ phòng: không được thu tiền cho phòng có thể đã bị người khác đặt
        if (r.Status != ReservationStatus.Pending || r.HoldExpiresAt == null || r.HoldExpiresAt <= now)
        {
            p.Status = PaymentStatus.Cancelled;
            p.Message = "Booking đã hết thời gian giữ phòng.";
            await _db.SaveChangesAsync();
            return ServiceResult<PaymentDto>.Fail("Đã hết thời gian giữ phòng. Vui lòng đặt lại.");
        }

        var result = await _gateway.ChargeAsync(new GatewayRequest
        {
            TransactionCode = p.TransactionCode,
            Amount = p.Amount,
            Method = p.Method,
            AccountNumber = accountNumber
        });

        p.CardLast4 = accountNumber[^4..];   // chỉ giữ 4 số cuối, KHÔNG lưu số đầy đủ
        p.Message = result.Message;

        switch (result.Outcome)
        {
            case GatewayOutcome.Success:
                p.Status = PaymentStatus.Paid;
                r.Status = ReservationStatus.Confirmed;   // cùng một lần SaveChanges với dòng trên
                r.HoldExpiresAt = null;
                break;

            case GatewayOutcome.Rejected:                 // E1
                p.Status = PaymentStatus.Failed;
                break;

            case GatewayOutcome.Timeout:                  // E2
                p.Status = PaymentStatus.Pending;
                r.HoldExpiresAt = now.AddMinutes(TimeoutExtraHoldMinutes);   // đừng nhả phòng khi chưa biết kết quả
                break;
        }

        await _db.SaveChangesAsync();   // lưu Payment và Reservation CÙNG LÚC: hoặc cả hai, hoặc không cái nào
        return ServiceResult<PaymentDto>.Ok(ToDto(p));
    }

    // ---------- UC-007 A2: khách hủy thanh toán ----------

    public async Task<ServiceResult<PaymentDto>> CancelAsync(int userId, int paymentId)
    {
        var p = await WithDetails()
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.Reservation!.UserId == userId);

        if (p == null)
            return ServiceResult<PaymentDto>.Missing();

        if (p.Status != PaymentStatus.Initiated)
            return ServiceResult<PaymentDto>.Fail("Giao dịch này không thể hủy.");

        p.Status = PaymentStatus.Cancelled;
        p.Message = "Khách hàng hủy thanh toán.";

        await _db.SaveChangesAsync();
        return ServiceResult<PaymentDto>.Ok(ToDto(p));
    }

    // ---------- UC-016 Payment History ----------

    public async Task<List<PaymentDto>> GetMineAsync(int userId)
    {
        var list = await WithDetails()
            .Where(p => p.Reservation!.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    // ---------- UC-020 Manage Payment (nhân viên) ----------

    public async Task<List<PaymentDto>> GetAllAsync(PaymentStatus? status)
    {
        var query = WithDetails();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        var list = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    // Đối soát giao dịch Pending (kết quả từ cổng thanh toán báo về sau)
    public async Task<ServiceResult<PaymentDto>> ReconcileAsync(int paymentId, bool success)
    {
        var p = await WithDetails().FirstOrDefaultAsync(x => x.Id == paymentId);
        if (p == null)
            return ServiceResult<PaymentDto>.Missing();

        if (p.Status != PaymentStatus.Pending)
            return ServiceResult<PaymentDto>.Fail("Chỉ đối soát được giao dịch đang chờ kết quả.");

        var r = p.Reservation!;

        if (success)
        {
            p.Status = PaymentStatus.Paid;
            if (r.Status == ReservationStatus.Pending)
            {
                r.Status = ReservationStatus.Confirmed;
                r.HoldExpiresAt = null;
                p.Message = "Đối soát: thanh toán thành công.";
            }
            else
            {
                // Booking đã không còn chờ thanh toán (ví dụ đã bị hủy): tiền đã thu nhưng không có booking
                p.Message = "Đối soát: thanh toán thành công nhưng booking không còn chờ thanh toán, cần hoàn tiền thủ công.";
            }
        }
        else
        {
            p.Status = PaymentStatus.Failed;
            p.Message = "Đối soát: thanh toán thất bại.";
        }

        await _db.SaveChangesAsync();
        return ServiceResult<PaymentDto>.Ok(ToDto(p));
    }
}