namespace LiBot.Application.Models.Aisle;

public class AisleWriteRequest(
    Guid FloorId,
    string Code,
    string Name,
    string? Description)
{
    public Guid FloorId { get; } = FloorId;
    public string Code { get; } = Code;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
}
