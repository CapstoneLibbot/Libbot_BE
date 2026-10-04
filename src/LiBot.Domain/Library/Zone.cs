using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Zone : Entity
{
    public Guid MapId { get; private set; }
    public string ZoneType { get; private set; }
    public double MaxSpeed { get; private set; }
    public string? PreferredDirection { get; private set; }
    public double? CorridorWidth { get; private set; }
    public bool AllowTurnaround { get; private set; }
    public double MinX { get; private set; }
    public double MinY { get; private set; }
    public double MaxX { get; private set; }
    public double MaxY { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Zone(Guid mapId, string zoneType, double minX, double minY, double maxX, double maxY,
        double maxSpeed = 0, string? preferredDirection = null, double? corridorWidth = null,
        bool allowTurnaround = false)
    {
        if (mapId == Guid.Empty)
            throw new ArgumentException("Map id is required.", nameof(mapId));
        if (string.IsNullOrWhiteSpace(zoneType))
            throw new ArgumentException("Zone type is required.", nameof(zoneType));
        if (maxX < minX || maxY < minY)
            throw new ArgumentException("Zone bounds are invalid.");
        if (maxSpeed < 0 || corridorWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(maxSpeed));

        MapId = mapId;
        ZoneType = zoneType.Trim();
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        MaxSpeed = maxSpeed;
        PreferredDirection = preferredDirection?.Trim();
        CorridorWidth = corridorWidth;
        AllowTurnaround = allowTurnaround;
    }

    public void Deactivate() => IsActive = false;
}