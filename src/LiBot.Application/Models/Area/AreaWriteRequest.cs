namespace LiBot.Application.Models.Area;

public class AreaWriteRequest(
    Guid FloorId,
    string Name,
    string? Description = null)
{
    public Guid FloorId { get; } = FloorId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
}
