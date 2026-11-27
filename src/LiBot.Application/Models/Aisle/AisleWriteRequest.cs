namespace LiBot.Application.Models.Aisle;

public class AisleWriteRequest(
    Guid FloorId,
    string Name,
    string? Description,
    string Status = "Active")
{
    public Guid FloorId { get; } = FloorId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public string Status { get; } = Status;
}
