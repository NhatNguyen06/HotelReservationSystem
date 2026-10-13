namespace HotelApi.Models;

public class PriceHistory
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }
    public RoomType? RoomType { get; set; }

    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public string? Reason { get; set; }

    public int ChangedByUserId { get; set; }
    public User? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }   // giờ UTC
}