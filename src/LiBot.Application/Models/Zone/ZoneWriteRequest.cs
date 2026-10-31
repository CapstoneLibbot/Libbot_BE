namespace LiBot.Application.Models.Zone;

public class ZoneWriteRequest(Guid MapId, string Name, string ZoneType, double MaxSpeed,
    double MinX, double MinY, double MaxX, double MaxY, string? Description = null,
    double? Width = null, double? Height = null, double RotationAngle = 0, bool IsActive = true)
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
    public double? Width { get; } = Width;
    public double? Height { get; } = Height;
    public double RotationAngle { get; } = RotationAngle;
    public bool IsActive { get; } = IsActive;
}
