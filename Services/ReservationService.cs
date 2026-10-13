using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class ReservationService : IReservationService
{
    private const int HoldMinutes = 15;          // thời gian giữ phòng
    private const int CancelDeadlineDays = 1;    // phải hủy/sửa trước ngày nhận phòng ít nhất 1 ngày

    private readonly AppDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly IBookingLock _bookingLock;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(AppDbContext db, IAvailabilityService availability,
        IBookingLock bookingLock, ILogger<ReservationService> logger)
    {
        _db = db;
        _availability = availability;
        _bookingLock = bookingLock;
        _logger = logger;
    }

    // ---------- Các hàm hỗ trợ ----------

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private static bool IsHoldExpired(Reservation r)
        => r.Status == ReservationStatus.Pending && r.HoldExpiresAt <= DateTime.UtcNow;

    private static bool WithinPolicy(Reservation r)
        => Today <= r.CheckIn.AddDays(-CancelDeadlineDays);

    private static string? ValidateStay(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn) return "Ngày trả phòng phải sau ngày nhận phòng.";
        if (checkIn < Today) return "Ngày nhận phòng không được ở quá khứ.";
        return null;
    }

    private static ReservationDto ToDto(Reservation r)
    {
        var expired = IsHoldExpired(r);
        var editable = r.Status == ReservationStatus.Confirmed && WithinPolicy(r);

        return new ReservationDto
        {
            Id = r.Id,
            BookingCode = r.BookingCode,
            RoomId = r.RoomId,
            RoomNumber = r.Room?.RoomNumber ?? string.Empty,
            RoomTypeName = r.Room?.RoomType?.Name ?? string.Empty,
            CheckIn = r.CheckIn,
            CheckOut = r.CheckOut,
            Nights = r.CheckOut.DayNumber - r.CheckIn.DayNumber,
            Guests = r.Guests,
            TotalCost = r.TotalCost,
            Status = expired ? "Expired" : r.Status.ToString(),
            // SpecifyKind để JSON có chữ "Z" (giờ UTC), trình duyệt mới đổi đúng giờ địa phương
            HoldExpiresAt = r.Status == ReservationStatus.Pending && !expired && r.HoldExpiresAt.HasValue
                ? DateTime.SpecifyKind(r.HoldExpiresAt.Value, DateTimeKind.Utc)
                : null,
            CanCancel = editable,
            CanModify = editable,
            HasPendingPayment = r.Payments.Any(p => p.Status == PaymentStatus.Pending)
        };
    }

    // Chỉ lấy booking CỦA CHÍNH người dùng này
    private IQueryable<Reservation> Mine(int userId)
        => _db.Reservations
            .Include(r => r.Room).ThenInclude(room => room!.RoomType)
            .Include(r => r.Payments)
            .Where(r => r.UserId == userId);

    // ---------- UC-006 Book Room ----------

    public async Task<ServiceResult<ReservationDto>> CreateAsync(int userId, CreateReservationDto dto)
    {
        var error = ValidateStay(dto.CheckIn, dto.CheckOut);
        if (error != null)
            return ServiceResult<ReservationDto>.Fail(error);

        // Từ đây đến khi lưu xong, chỉ một request được chạy: tránh hai người cùng đặt một phòng
        using var guard = await _bookingLock.AcquireAsync();

        var room = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == dto.RoomId);
        if (room == null)
            return ServiceResult<ReservationDto>.Fail("Phòng không tồn tại.");

        if (dto.Guests > room.RoomType!.Capacity)
            return ServiceResult<ReservationDto>.Fail($"Phòng này chứa tối đa {room.RoomType.Capacity} khách.");

        // UC-004: kiểm tra phòng trống (tái sử dụng hàm của Giai đoạn 2)
        if (!await _availability.IsRoomAvailableAsync(room.Id, dto.CheckIn, dto.CheckOut))
            return ServiceResult<ReservationDto>.Fail("Phòng đã được đặt trong thời gian này. Vui lòng chọn phòng hoặc ngày khác.");

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;

        var reservation = new Reservation
        {
            BookingCode = "BK-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            UserId = userId,
            RoomId = room.Id,
            CheckIn = dto.CheckIn,
            CheckOut = dto.CheckOut,
            Guests = dto.Guests,
            TotalCost = nights * room.RoomType.BasePrice,
            Status = ReservationStatus.Pending,
            HoldExpiresAt = DateTime.UtcNow.AddMinutes(HoldMinutes)   // giữ phòng tạm
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Tạo booking {Code}: phòng {Room}, {In} → {Out}, user {UserId}",
            reservation.BookingCode, room.RoomNumber, dto.CheckIn, dto.CheckOut, userId);

        reservation.Room = room;   // room đã load kèm RoomType, dùng cho ToDto
        return ServiceResult<ReservationDto>.Ok(ToDto(reservation));
    }

    // ---------- UC-008 View Booking ----------

    public async Task<List<ReservationDto>> GetMineAsync(int userId)
    {
        var list = await Mine(userId)
            .OrderByDescending(r => r.CheckIn)
            .ToListAsync();

        return list.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<ReservationDto>> GetMineByIdAsync(int userId, int id)
    {
        var r = await Mine(userId).FirstOrDefaultAsync(x => x.Id == id);
        return r == null
            ? ServiceResult<ReservationDto>.Missing()
            : ServiceResult<ReservationDto>.Ok(ToDto(r));
    }

    // ---------- UC-009 Cancel Booking ----------

    public async Task<ServiceResult<ReservationDto>> CancelAsync(int userId, int id, string? reason)
    {
        var r = await Mine(userId).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<ReservationDto>.Missing();

        // E2: trạng thái không cho hủy
        if (r.Status == ReservationStatus.Pending)
            return ServiceResult<ReservationDto>.Fail("Booking đang chờ thanh toán và sẽ tự hết hạn nếu không thanh toán.");

        if (r.Status != ReservationStatus.Confirmed)
            return ServiceResult<ReservationDto>.Fail($"Booking đang ở trạng thái {r.Status} nên không thể hủy.");

        // E1: quá hạn hủy theo chính sách
        if (!WithinPolicy(r))
            return ServiceResult<ReservationDto>.Fail("Đã quá hạn hủy theo chính sách (phải hủy trước ngày nhận phòng ít nhất 1 ngày). Vui lòng liên hệ khách sạn.");

        r.Status = ReservationStatus.Cancelled;
        r.CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        r.HoldExpiresAt = null;

        // "Giải phóng phòng" không cần làm gì thêm: phòng trống được tính từ bảng Reservation,
        // booking đã Cancelled thì không còn chiếm phòng (xem OverlappingActive ở Giai đoạn 2)
        await _db.SaveChangesAsync();

        _logger.LogInformation("Hủy booking {Code} (user {UserId})", r.BookingCode, userId);
        return ServiceResult<ReservationDto>.Ok(ToDto(r));
    }

    // ---------- UC-014 Modify Booking ----------

    public async Task<ServiceResult<ReservationDto>> ModifyAsync(int userId, int id, ModifyReservationDto dto)
    {
        // Khóa từ đầu để dữ liệu đọc lên luôn là mới nhất và không ai chen vào giữa chừng
        using var guard = await _bookingLock.AcquireAsync();

        var r = await Mine(userId).FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<ReservationDto>.Missing();

        if (r.Status != ReservationStatus.Confirmed || !WithinPolicy(r))
            return ServiceResult<ReservationDto>.Fail("Booking này không thể chỉnh sửa (chưa được xác nhận hoặc đã quá hạn chỉnh sửa).");

        var error = ValidateStay(dto.CheckIn, dto.CheckOut);
        if (error != null)
            return ServiceResult<ReservationDto>.Fail(error);

        var capacity = r.Room!.RoomType!.Capacity;
        if (dto.Guests > capacity)
            return ServiceResult<ReservationDto>.Fail($"Phòng này chứa tối đa {capacity} khách.");

        // excludeReservationId: không coi chính booking này là xung đột với nó
        if (!await _availability.IsRoomAvailableAsync(r.RoomId, dto.CheckIn, dto.CheckOut, excludeReservationId: r.Id))
            return ServiceResult<ReservationDto>.Fail("Phòng không còn trống trong khoảng thời gian mới.");

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;
        var newTotal = nights * r.Room.RoomType.BasePrice;

        if (newTotal > r.TotalCost)
            return ServiceResult<ReservationDto>.Fail(
                $"Tổng tiền mới ({newTotal:N0} đ) cao hơn tiền đã thanh toán ({r.TotalCost:N0} đ). " +
                "Tính năng thanh toán phần chênh lệch sẽ được bổ sung ở giai đoạn thanh toán.");

        r.CheckIn = dto.CheckIn;
        r.CheckOut = dto.CheckOut;
        r.Guests = dto.Guests;
        r.TotalCost = newTotal;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Sửa booking {Code}: {In} → {Out}, tổng {Total}", r.BookingCode, r.CheckIn, r.CheckOut, newTotal);
        return ServiceResult<ReservationDto>.Ok(ToDto(r));
    }
}