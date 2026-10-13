using HotelApi.Data;
using HotelApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelApi.Services;

public class StaffReservationService : IStaffReservationService
{
    private static readonly TimeSpan StandardCheckOutTime = new(12, 0, 0);   // giờ trả phòng tiêu chuẩn
    private const decimal LateSurchargeRate = 0.5m;                          // phụ thu = 50% giá một đêm

    private readonly AppDbContext _db;
    private readonly IAvailabilityService _availability;
    private readonly IBookingLock _bookingLock;
    private readonly ILogger<StaffReservationService> _logger;

    public StaffReservationService(AppDbContext db, IAvailabilityService availability,
        IBookingLock bookingLock, ILogger<StaffReservationService> logger)
    {
        _db = db;
        _availability = availability;
        _bookingLock = bookingLock;
        _logger = logger;
    }

    // ---------- Hàm hỗ trợ ----------

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private IQueryable<Reservation> Full()
        => _db.Reservations
            .Include(r => r.Room).ThenInclude(room => room!.RoomType)
            .Include(r => r.User)
            .Include(r => r.Payments)
            .Include(r => r.Invoice);

    private static decimal PaidAmount(Reservation r)
        => r.Payments.Where(p => p.Status == PaymentStatus.Paid).Sum(p => p.Amount);

    private static string GuestNameOf(Reservation r) => r.GuestName ?? r.User?.FullName ?? string.Empty;

    private static DateTime? Utc(DateTime? d)
        => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null;

    private static string? ValidateStay(DateOnly checkIn, DateOnly checkOut)
    {
        if (checkOut <= checkIn) return "Ngày trả phòng phải sau ngày nhận phòng.";
        if (checkIn < Today) return "Ngày nhận phòng không được ở quá khứ.";
        return null;
    }

    private static string DescribeStatus(ReservationStatus s) => s switch
    {
        ReservationStatus.Pending => "đang chờ thanh toán",
        ReservationStatus.Confirmed => "đã xác nhận",
        ReservationStatus.CheckedIn => "đã check-in",
        ReservationStatus.CheckedOut => "đã trả phòng",
        ReservationStatus.Cancelled => "đã bị hủy",
        _ => s.ToString()
    };

    private static StaffReservationDto ToDto(Reservation r)
    {
        var expired = r.Status == ReservationStatus.Pending && r.HoldExpiresAt <= DateTime.UtcNow;
        var isActive = r.Status == ReservationStatus.Confirmed
                       || (r.Status == ReservationStatus.Pending && !expired);
        var today = Today;

        return new StaffReservationDto
        {
            Id = r.Id,
            BookingCode = r.BookingCode,
            GuestName = GuestNameOf(r),
            GuestPhone = r.GuestPhone ?? r.User?.Phone ?? string.Empty,
            IsOnline = r.UserId != null,
            RoomId = r.RoomId,
            RoomNumber = r.Room?.RoomNumber ?? string.Empty,
            RoomTypeName = r.Room?.RoomType?.Name ?? string.Empty,
            CheckIn = r.CheckIn,
            CheckOut = r.CheckOut,
            Nights = r.CheckOut.DayNumber - r.CheckIn.DayNumber,
            Guests = r.Guests,
            TotalCost = r.TotalCost,
            PaidAmount = PaidAmount(r),
            Status = expired ? "Expired" : r.Status.ToString(),
            CheckInTime = Utc(r.CheckInTime),
            CheckOutTime = Utc(r.CheckOutTime),
            CanCheckIn = r.Status == ReservationStatus.Confirmed && today >= r.CheckIn && today < r.CheckOut,
            CanCheckOut = r.Status == ReservationStatus.CheckedIn,
            CanEdit = r.Status == ReservationStatus.Confirmed,
            CanCancel = isActive,
            HasInvoice = r.Invoice != null
        };
    }

    private static InvoiceDto ToInvoiceDto(Invoice i, Reservation r) => new()
    {
        Id = i.Id,
        InvoiceNumber = i.InvoiceNumber,
        BookingCode = r.BookingCode,
        GuestName = GuestNameOf(r),
        RoomNumber = r.Room?.RoomNumber ?? string.Empty,
        RoomTypeName = r.Room?.RoomType?.Name ?? string.Empty,
        CheckIn = r.CheckIn,
        CheckOut = r.CheckOut,
        Nights = r.CheckOut.DayNumber - r.CheckIn.DayNumber,
        RoomCharge = i.RoomCharge,
        ExtraCharge = i.ExtraCharge,
        ExtraChargeNote = i.ExtraChargeNote,
        Total = i.Total,
        PaidBefore = i.PaidBefore,
        PaidAtCheckout = i.PaidAtCheckout,
        RefundDue = Math.Max(0, i.PaidBefore + i.PaidAtCheckout - i.Total),
        IssuedAt = DateTime.SpecifyKind(i.IssuedAt, DateTimeKind.Utc)
    };

    // ---------- UC-011: tìm, xem ----------

    public async Task<List<StaffReservationDto>> SearchAsync(string? code, string? guest, DateOnly? date, string? status)
    {
        var query = Full();

        if (!string.IsNullOrWhiteSpace(code))
        {
            var c = code.Trim().ToLower();
            query = query.Where(r => r.BookingCode.ToLower().Contains(c));
        }

        if (!string.IsNullOrWhiteSpace(guest))
        {
            var g = guest.Trim().ToLower();
            query = query.Where(r =>
                (r.GuestName != null && r.GuestName.ToLower().Contains(g)) ||
                (r.GuestPhone != null && r.GuestPhone.Contains(g)) ||
                (r.User != null && (r.User.FullName.ToLower().Contains(g) || r.User.Phone.Contains(g))));
        }

        if (date.HasValue)
            query = query.Where(r => r.CheckIn <= date.Value && r.CheckOut > date.Value);   // booking đang ở trong ngày này

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReservationStatus>(status, true, out var st))
            query = query.Where(r => r.Status == st);

        var list = await query.OrderByDescending(r => r.CheckIn).ToListAsync();
        return list.Select(ToDto).ToList();
    }

    public async Task<ServiceResult<StaffReservationDto>> GetAsync(int id)
    {
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        return r == null
            ? ServiceResult<StaffReservationDto>.Missing()
            : ServiceResult<StaffReservationDto>.Ok(ToDto(r));
    }

    // ---------- UC-011: tạo booking tại quầy ----------

    public async Task<ServiceResult<StaffReservationDto>> CreateAsync(StaffReservationInputDto dto)
    {
        using var guard = await _bookingLock.AcquireAsync();
        var error = ValidateStay(dto.CheckIn, dto.CheckOut);
        if (error != null)
            return ServiceResult<StaffReservationDto>.Fail(error);

        var room = await _db.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == dto.RoomId);
        if (room == null)
            return ServiceResult<StaffReservationDto>.Fail("Phòng không tồn tại.");

        if (dto.Guests > room.RoomType!.Capacity)
            return ServiceResult<StaffReservationDto>.Fail($"Phòng này chứa tối đa {room.RoomType.Capacity} khách.");

        // UC-011 bước 6, E1: kiểm tra phòng trống
        if (!await _availability.IsRoomAvailableAsync(room.Id, dto.CheckIn, dto.CheckOut))
            return ServiceResult<StaffReservationDto>.Fail("Phòng không còn trống trong thời gian này. Hãy chọn phòng hoặc ngày khác.");

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;

        var reservation = new Reservation
        {
            BookingCode = "BK-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            UserId = null,                              // khách vãng lai, không có tài khoản
            GuestName = dto.GuestName.Trim(),
            GuestPhone = dto.GuestPhone,
            RoomId = room.Id,
            CheckIn = dto.CheckIn,
            CheckOut = dto.CheckOut,
            Guests = dto.Guests,
            TotalCost = nights * room.RoomType.BasePrice,
            Status = ReservationStatus.Confirmed         // đặt trực tiếp: xác nhận ngay, thu tiền khi check-out
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        reservation.Room = room;
        return ServiceResult<StaffReservationDto>.Ok(ToDto(reservation));
    }

    // ---------- UC-011: sửa ----------

    public async Task<ServiceResult<StaffReservationDto>> UpdateAsync(int id, StaffReservationInputDto dto)
    {
        using var guard = await _bookingLock.AcquireAsync();   // tránh đặt trùng phòng
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<StaffReservationDto>.Missing();

        // E3: đã check-in, check-out hoặc hủy thì không sửa; booking chờ thanh toán thì để khách thanh toán trước
        if (r.Status != ReservationStatus.Confirmed)
            return ServiceResult<StaffReservationDto>.Fail(
                $"Booking {DescribeStatus(r.Status)} nên không thể chỉnh sửa. Chỉ sửa được booking đã xác nhận.");

        var error = ValidateStay(dto.CheckIn, dto.CheckOut);
        if (error != null)
            return ServiceResult<StaffReservationDto>.Fail(error);

        var room = await _db.Rooms.Include(x => x.RoomType).FirstOrDefaultAsync(x => x.Id == dto.RoomId);
        if (room == null)
            return ServiceResult<StaffReservationDto>.Fail("Phòng không tồn tại.");

        if (dto.Guests > room.RoomType!.Capacity)
            return ServiceResult<StaffReservationDto>.Fail($"Phòng này chứa tối đa {room.RoomType.Capacity} khách.");

        // excludeReservationId: không coi chính booking này là xung đột với nó
        if (!await _availability.IsRoomAvailableAsync(room.Id, dto.CheckIn, dto.CheckOut, r.Id))
            return ServiceResult<StaffReservationDto>.Fail("Phòng không còn trống trong khoảng thời gian mới.");

        var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;

        r.GuestName = dto.GuestName.Trim();
        r.GuestPhone = dto.GuestPhone;
        r.RoomId = room.Id;
        r.Room = room;
        r.CheckIn = dto.CheckIn;
        r.CheckOut = dto.CheckOut;
        r.Guests = dto.Guests;
        r.TotalCost = nights * room.RoomType.BasePrice;   // nếu tăng, phần chênh được thu khi check-out

        await _db.SaveChangesAsync();
        return ServiceResult<StaffReservationDto>.Ok(ToDto(r));
    }

    // ---------- UC-011 A2: hủy ----------

    public async Task<ServiceResult<StaffReservationDto>> CancelAsync(int id, string? reason)
    {
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<StaffReservationDto>.Missing();

        if (r.Status != ReservationStatus.Pending && r.Status != ReservationStatus.Confirmed)
            return ServiceResult<StaffReservationDto>.Fail($"Booking {DescribeStatus(r.Status)} nên không thể hủy.");

        r.Status = ReservationStatus.Cancelled;
        r.CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        r.HoldExpiresAt = null;

        // Đóng các phiên thanh toán còn mở để khách không thể trả tiền cho booking đã hủy
        foreach (var p in r.Payments.Where(p => p.Status == PaymentStatus.Initiated))
        {
            p.Status = PaymentStatus.Cancelled;
            p.Message = "Booking đã bị hủy.";
        }

        // "Giải phóng phòng": phòng trống được tính từ bảng Reservation nên không cần làm gì thêm
        await _db.SaveChangesAsync();
        return ServiceResult<StaffReservationDto>.Ok(ToDto(r));
    }

    // ---------- UC-017 A3: gợi ý phòng tương đương ----------

    public async Task<ServiceResult<List<AvailableRoomDto>>> GetAlternativeRoomsAsync(int id)
    {
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<List<AvailableRoomDto>>.Missing();

        var rooms = await _availability.SearchAsync(r.CheckIn, r.CheckOut, r.Guests);
        var typeId = r.Room!.RoomTypeId;

        // Cùng loại phòng, không phải phòng hiện tại, đã dọn xong (Available)
        var result = rooms
            .Where(x => x.RoomTypeId == typeId && x.Id != r.RoomId && x.Status == "Available")
            .ToList();

        return ServiceResult<List<AvailableRoomDto>>.Ok(result);
    }

    // ---------- UC-017: check-in ----------

    public async Task<ServiceResult<StaffReservationDto>> CheckInAsync(int id, CheckInDto dto)
    {
        using var guard = await _bookingLock.AcquireAsync();   // vì check-in có thể đổi sang phòng khác

        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<StaffReservationDto>.Missing();

        // E2: trạng thái không cho phép check-in
        if (r.Status != ReservationStatus.Confirmed)
            return ServiceResult<StaffReservationDto>.Fail($"Không thể check-in: booking {DescribeStatus(r.Status)}.");

        var today = Today;
        if (today < r.CheckIn)
            return ServiceResult<StaffReservationDto>.Fail($"Chưa đến ngày nhận phòng ({r.CheckIn:dd/MM/yyyy}).");
        if (today >= r.CheckOut)
            return ServiceResult<StaffReservationDto>.Fail("Booking đã quá thời gian lưu trú.");

        // E3: phải đối chiếu giấy tờ tùy thân
        if (!dto.IdentityVerified)
            return ServiceResult<StaffReservationDto>.Fail(
                "Cần đối chiếu giấy tờ tùy thân của khách với thông tin booking trước khi check-in.");

        // A3: đổi sang phòng tương đương
        var room = r.Room!;
        if (dto.RoomId.HasValue && dto.RoomId.Value != r.RoomId)
        {
            var target = await _db.Rooms.Include(x => x.RoomType).FirstOrDefaultAsync(x => x.Id == dto.RoomId.Value);
            if (target == null)
                return ServiceResult<StaffReservationDto>.Fail("Phòng được chọn không tồn tại.");
            if (target.RoomTypeId != room.RoomTypeId)
                return ServiceResult<StaffReservationDto>.Fail("Chỉ được đổi sang phòng cùng loại (tương đương).");
            if (!await _availability.IsRoomAvailableAsync(target.Id, r.CheckIn, r.CheckOut, r.Id))
                return ServiceResult<StaffReservationDto>.Fail("Phòng được chọn đã có booking khác trong thời gian này.");

            room = target;
        }

        // Bước 5, E4: phòng phải sẵn sàng (sạch và trống)
        if (room.Status != RoomStatus.Available)
            return ServiceResult<StaffReservationDto>.Fail(
                $"Phòng {room.RoomNumber} chưa sẵn sàng (trạng thái: {room.Status}). Hãy chọn phòng tương đương khác hoặc báo quản lý.");

        r.RoomId = room.Id;
        r.Room = room;
        r.Status = ReservationStatus.CheckedIn;
        r.CheckInTime = DateTime.UtcNow;
        room.Status = RoomStatus.Occupied;

        await _db.SaveChangesAsync();   // booking và phòng đổi CÙNG LÚC (E5: lỗi thì không đổi gì)
        return ServiceResult<StaffReservationDto>.Ok(ToDto(r));
    }

    // ---------- UC-018: xem trước khi check-out ----------

    public async Task<ServiceResult<CheckOutPreviewDto>> PreviewCheckOutAsync(int id)
    {
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<CheckOutPreviewDto>.Missing();

        if (r.Status != ReservationStatus.CheckedIn)
            return ServiceResult<CheckOutPreviewDto>.Fail($"Không thể check-out: booking {DescribeStatus(r.Status)}.");

        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var isLate = today > r.CheckOut || (today == r.CheckOut && now.TimeOfDay > StandardCheckOutTime);   // A2

        return ServiceResult<CheckOutPreviewDto>.Ok(new CheckOutPreviewDto
        {
            RoomCharge = r.TotalCost,
            PaidAmount = PaidAmount(r),
            IsLate = isLate,
            SuggestedLateSurcharge = isLate ? Math.Round(r.Room!.RoomType!.BasePrice * LateSurchargeRate, 0) : 0,
            StandardCheckOutTime = "12:00"
        });
    }

    // ---------- UC-018 + UC-019: check-out và tạo hóa đơn ----------

    public async Task<ServiceResult<InvoiceDto>> CheckOutAsync(int id, CheckOutDto dto)
    {
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null)
            return ServiceResult<InvoiceDto>.Missing();

        // E1
        if (r.Status != ReservationStatus.CheckedIn)
            return ServiceResult<InvoiceDto>.Fail($"Không thể check-out: booking {DescribeStatus(r.Status)}.");

        var now = DateTime.UtcNow;
        var total = r.TotalCost + dto.ExtraCharge;
        var paidBefore = PaidAmount(r);
        var balance = total - paidBefore;
        decimal paidAtCheckout = 0;

        if (balance > 0)
        {
            // E3: còn nợ mà chưa thu được thì KHÔNG hoàn tất check-out, booking giữ nguyên CheckedIn
            if (!dto.CollectBalance)
                return ServiceResult<InvoiceDto>.Fail(
                    $"Khách còn nợ {balance:N0} đ. Cần thu khoản này để hoàn tất check-out, hoặc báo quản lý xử lý.");

            _db.Payments.Add(new Payment
            {
                ReservationId = r.Id,
                Amount = balance,
                Method = dto.BalanceMethod,
                Status = PaymentStatus.Paid,
                TransactionCode = "TX-" + Guid.NewGuid().ToString("N")[..10].ToUpper(),
                Message = "Thu phần còn lại khi check-out.",
                CreatedAt = now,
                SessionExpiresAt = now
            });
            paidAtCheckout = balance;
        }

        // UC-019: tạo hóa đơn
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            ReservationId = r.Id,
            RoomCharge = r.TotalCost,
            ExtraCharge = dto.ExtraCharge,
            ExtraChargeNote = string.IsNullOrWhiteSpace(dto.ExtraChargeNote) ? null : dto.ExtraChargeNote.Trim(),
            Total = total,
            PaidBefore = paidBefore,
            PaidAtCheckout = paidAtCheckout,
            IssuedAt = now
        };
        _db.Invoices.Add(invoice);
        r.Invoice = invoice;

        r.Status = ReservationStatus.CheckedOut;
        r.CheckOutTime = now;
        r.Room!.Status = RoomStatus.NeedsCleaning;

        // Hóa đơn, thanh toán, booking, phòng: cùng MỘT lần lưu. E4: lỗi tạo hóa đơn thì không check-out
        await _db.SaveChangesAsync();
        return ServiceResult<InvoiceDto>.Ok(ToInvoiceDto(invoice, r));
    }

    public async Task<ServiceResult<InvoiceDto>> GetInvoiceAsync(int id)
    {
        var r = await Full().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null || r.Invoice == null)
            return ServiceResult<InvoiceDto>.Missing();

        return ServiceResult<InvoiceDto>.Ok(ToInvoiceDto(r.Invoice, r));
    }
}