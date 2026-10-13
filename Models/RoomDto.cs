using System.ComponentModel.DataAnnotations;

namespace HotelApi.Models;

// DTO trả ra ngoài
public class RoomTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
    public string Amenities { get; set; } = string.Empty;
}

public class RoomDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
    public string Amenities { get; set; } = string.Empty;
}

public class AvailableRoomDto : RoomDto
{
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
}

public class AvailabilityResultDto
{
    public bool Available { get; set; }
    public int Nights { get; set; }
    public decimal TotalPrice { get; set; }
}

// DTO nhận vào
public class CreateRoomDto
{
    [Required(ErrorMessage = "Số phòng là bắt buộc")]
    [StringLength(10, ErrorMessage = "Số phòng tối đa 10 ký tự")]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Phải chọn loại phòng")]
    public int RoomTypeId { get; set; }
}

public class UpdateRoomDto
{
    [Required(ErrorMessage = "Số phòng là bắt buộc")]
    [StringLength(10, ErrorMessage = "Số phòng tối đa 10 ký tự")]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Phải chọn loại phòng")]
    public int RoomTypeId { get; set; }
}

public class UpdateRoomStatusDto
{
    public RoomStatus Status { get; set; }
}