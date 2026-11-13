namespace LiBot.Application.Models.Zone;

public class ZoneResponse(Guid Id, Guid MapId, string Name, string? Description, string ZoneType, double MaxSpeed,
    double MinX, double MinY, double MaxX, double MaxY, bool IsActive,
    DateTime CreatedAtUtc, ZoneMapResponse? Map = null)
{
    public Guid Id { get; } = Id;
    public Guid MapId { get; } = MapId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public string ZoneType { get; } = ZoneType;
    public double MaxSpeed { get; } = MaxSpeed;
    public double MinX { get; } = MinX;
    public double MinY { get; } = MinY;
    public double MaxX { get; } = MaxX;
    public double MaxY { get; } = MaxY;
    public bool IsActive { get; } = IsActive;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public ZoneMapResponse? Map { get; } = Map;
}
