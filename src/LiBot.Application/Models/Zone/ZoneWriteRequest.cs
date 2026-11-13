namespace LiBot.Application.Models.Zone;

public class ZoneWriteRequest(Guid MapId, string Name, string ZoneType, double MaxSpeed,
    double MinX, double MinY, double MaxX, double MaxY, string? Description = null, bool IsActive = true)
{
    public Guid MapId { get; } = MapId;
    public string Name { get; } = Name;
    public string ZoneType { get; } = ZoneType;
    public double MaxSpeed { get; } = MaxSpeed;
    public double MinX { get; } = MinX;
    public double MinY { get; } = MinY;
    public double MaxX { get; } = MaxX;
    public double MaxY { get; } = MaxY;
    public string? Description { get; } = Description;
    public bool IsActive { get; } = IsActive;
}
