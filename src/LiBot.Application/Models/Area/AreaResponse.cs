namespace LiBot.Application.Models.Area;

public class AreaResponse(
    Guid Id,
    Guid FloorId,
    string Name,
    string? Description,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    AreaFloorResponse? Floor = null)
{
    public Guid Id { get; } = Id;
    public Guid FloorId { get; } = FloorId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
    public AreaFloorResponse? Floor { get; } = Floor;
}
