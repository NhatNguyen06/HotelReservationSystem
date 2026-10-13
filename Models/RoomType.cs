namespace HotelApi.Models;

public class RoomType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int Capacity { get; set; }
    public string Amenities { get; set; } = string.Empty;

    public List<Room> Rooms { get; set; } = new();
}