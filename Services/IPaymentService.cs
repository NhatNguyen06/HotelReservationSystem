using HotelApi.Models;

namespace HotelApi.Services;

public interface IPaymentService
{
    // Khách hàng
    Task<ServiceResult<PaymentDto>> StartAsync(int userId, StartPaymentDto dto);
    Task<ServiceResult<PaymentDto>> ConfirmAsync(int userId, int paymentId, string accountNumber);
    Task<ServiceResult<PaymentDto>> CancelAsync(int userId, int paymentId);
    Task<List<PaymentDto>> GetMineAsync(int userId);

    // Nhân viên (UC-020)
    Task<List<PaymentDto>> GetAllAsync(PaymentStatus? status);
    Task<ServiceResult<PaymentDto>> ReconcileAsync(int paymentId, bool success);
}