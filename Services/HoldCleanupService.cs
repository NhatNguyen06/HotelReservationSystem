using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

// Chạy nền mỗi phút: hủy các booking Pending đã hết hạn giữ phòng
public class HoldCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldCleanupService> _logger;

    public HoldCleanupService(IServiceScopeFactory scopeFactory, ILogger<HoldCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dọn booking hết hạn giữ phòng");   // một lần lỗi không làm dừng tác vụ
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;   // ứng dụng đang tắt
            }
        }
    }

    private async Task CleanAsync(CancellationToken ct)
    {
        // Tác vụ nền sống suốt đời ứng dụng (Singleton) còn AppDbContext là Scoped,
        // nên mỗi lần chạy phải tự tạo một "scope" riêng, giống một request nhỏ
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var expired = await db.Reservations
            .Include(r => r.Payments)
            .Where(r => r.Status == ReservationStatus.Pending && r.HoldExpiresAt <= now)
            .ToListAsync(ct);

        // Booking còn giao dịch đang chờ đối soát thì để nhân viên xử lý, không tự hủy
        expired = expired.Where(r => !r.Payments.Any(p => p.Status == PaymentStatus.Pending)).ToList();
        if (expired.Count == 0)
            return;

        foreach (var r in expired)
        {
            r.Status = ReservationStatus.Cancelled;
            r.CancelReason = "Hết thời gian giữ phòng";
            r.HoldExpiresAt = null;

            foreach (var p in r.Payments.Where(p => p.Status == PaymentStatus.Initiated))
            {
                p.Status = PaymentStatus.Cancelled;
                p.Message = "Booking hết hạn giữ phòng.";
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Đã hủy {Count} booking hết hạn giữ phòng", expired.Count);
    }
}