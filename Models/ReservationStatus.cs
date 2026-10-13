namespace HotelApi.Models;

public enum ReservationStatus
{
    Pending,      // đang giữ phòng, chờ thanh toán
    Confirmed,    // đã thanh toán, đã xác nhận
    CheckedIn,
    CheckedOut,
    Cancelled
}