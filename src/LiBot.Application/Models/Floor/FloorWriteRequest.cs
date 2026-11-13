namespace LiBot.Application.Models.Floor;

public class FloorWriteRequest(string Name, int FloorNumber, string? Description = null, string Status = "Active")
{
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
}
