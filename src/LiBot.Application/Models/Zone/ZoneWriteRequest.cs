namespace LiBot.Application.Models.Zone;

public class ZoneWriteRequest(Guid MapId, string ZoneType, double MaxSpeed,
    string? PreferredDirection, double? CorridorWidth, bool AllowTurnaround,
    double MinX, double MinY, double MaxX, double MaxY, bool IsActive = true)
{
    public Guid MapId { get; } = MapId;
    public string ZoneType { get; } = ZoneType;
    public double MaxSpeed { get; } = MaxSpeed;
    public string? PreferredDirection { get; } = PreferredDirection;
    public double? CorridorWidth { get; } = CorridorWidth;
    public bool AllowTurnaround { get; } = AllowTurnaround;
    public double MinX { get; } = MinX;
    public double MinY { get; } = MinY;
    public double MaxX { get; } = MaxX;
    public double MaxY { get; } = MaxY;
    public bool IsActive { get; } = IsActive;
}
