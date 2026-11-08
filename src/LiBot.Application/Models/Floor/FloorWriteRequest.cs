using LiBot.Domain.Enums;

namespace LiBot.Application.Models.Floor;

public class FloorWriteRequest(string Name, int FloorNumber, string? Description = null, FloorStatus? Status = null)
{
    public string Name { get; } = Name;
    public int FloorNumber { get; } = FloorNumber;
    public string? Description { get; } = Description;
    public FloorStatus? Status { get; } = Status;
}
