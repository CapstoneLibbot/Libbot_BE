namespace LiBot.Application.Models.Area;

public class AreaFloorResponse(Guid Id, string Name, int FloorNumber)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
}
