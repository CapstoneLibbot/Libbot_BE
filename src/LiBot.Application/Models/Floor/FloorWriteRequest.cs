namespace LiBot.Application.Models.Floor;

public class FloorWriteRequest(string Name, int FloorNumber)
{
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
}
