using HotelApi.Models;
using HotelApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]   // mặc định: phải đăng nhập. Từng action siết thêm theo vai trò
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;

    public PaymentsController(IPaymentService payments)
    {
        _payments = payments;
    }

    // ----- Khách hàng -----

    // POST /api/payments  (tạo phiên thanh toán)
    [Authorize(Roles = Roles.Customer)]
    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Start(StartPaymentDto dto)
    {
        var result = await _payments.StartAsync(User.GetUserId(), dto);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return StatusCode(201, result.Data);
    }

    // POST /api/payments/5/confirm
    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<PaymentDto>> Confirm(int id, ConfirmPaymentDto dto)
    {
        var result = await _payments.ConfirmAsync(User.GetUserId(), id, dto.AccountNumber);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // Kể cả khi cổng từ chối hay timeout vẫn trả 200: kết quả nằm trong trường "status"
        return Ok(result.Data);
    }

    // POST /api/payments/5/cancel
    [Authorize(Roles = Roles.Customer)]
    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PaymentDto>> Cancel(int id)
    {
        var result = await _payments.CancelAsync(User.GetUserId(), id);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }

    // GET /api/payments/my  (UC-016)
    [Authorize(Roles = Roles.Customer)]
    [HttpGet("my")]
    public async Task<ActionResult<List<PaymentDto>>> GetMine()
    {
        return Ok(await _payments.GetMineAsync(User.GetUserId()));
    }

    // ----- Nhân viên (UC-020) -----

    // GET /api/payments?status=Pending
    [Authorize(Roles = Roles.StaffAndManager)]
    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetAll([FromQuery] PaymentStatus? status)
    {
        return Ok(await _payments.GetAllAsync(status));
    }

    // POST /api/payments/5/reconcile
    [Authorize(Roles = Roles.StaffAndManager)]
    [HttpPost("{id:int}/reconcile")]
    public async Task<ActionResult<PaymentDto>> Reconcile(int id, ReconcilePaymentDto dto)
    {
        var result = await _payments.ReconcileAsync(id, dto.Success);
        if (result.NotFound)
            return NotFound();
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(result.Data);
    }
}