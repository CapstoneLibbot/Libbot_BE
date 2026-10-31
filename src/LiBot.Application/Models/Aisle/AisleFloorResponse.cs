namespace LiBot.Application.Models.Aisle;

public class AisleFloorResponse(Guid Id, string Name, int FloorNumber)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
}
