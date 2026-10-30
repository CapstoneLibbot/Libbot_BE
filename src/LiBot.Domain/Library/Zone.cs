using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Zone : Entity
{
    public static readonly IReadOnlySet<string> SupportedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "KEEP_OUT",
        "SPEED_LIMIT",
        "NO_STOP"
    };

    public Guid MapId { get; private set; }
    public string ZoneType { get; private set; }
    public double MaxSpeed { get; private set; }
    public double MinX { get; private set; }
    public double MinY { get; private set; }
    public double MaxX { get; private set; }
    public double MaxY { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Zone(
        Guid mapId,
        string zoneType,
        double minX,
        double minY,
        double maxX,
        double maxY,
        double maxSpeed = 0)
    {
        if (mapId == Guid.Empty)
            throw new ArgumentException("Map id is required.", nameof(mapId));
        if (string.IsNullOrWhiteSpace(zoneType))
            throw new ArgumentException("Zone type is required.", nameof(zoneType));
        if (!double.IsFinite(minX) || !double.IsFinite(minY) || !double.IsFinite(maxX) || !double.IsFinite(maxY))
            throw new ArgumentException("Zone bounds must be finite.");
        if (maxX <= minX || maxY <= minY)
            throw new ArgumentException("Zone bounds must have positive width and height.");
        var normalizedType = zoneType.Trim().ToUpperInvariant();
        if (!SupportedTypes.Contains(normalizedType))
            throw new ArgumentException($"Unsupported zone type '{zoneType}'.", nameof(zoneType));
        if (normalizedType == "SPEED_LIMIT" && (!double.IsFinite(maxSpeed) || maxSpeed <= 0))
            throw new ArgumentException("SPEED_LIMIT requires maxSpeed greater than zero.", nameof(maxSpeed));

        MapId = mapId;
        ZoneType = normalizedType;
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        MaxSpeed = normalizedType == "SPEED_LIMIT" ? maxSpeed : 0;
    }

    public void Deactivate() => IsActive = false;

    public void Update(Guid mapId, string zoneType, double minX, double minY, double maxX, double maxY,
        double maxSpeed, string? preferredDirection, double? corridorWidth, bool allowTurnaround, bool isActive)
    {
        var updated = new Zone(mapId, zoneType, minX, minY, maxX, maxY, maxSpeed, preferredDirection, corridorWidth, allowTurnaround);
        MapId = updated.MapId;
        ZoneType = updated.ZoneType;
        MinX = updated.MinX;
        MinY = updated.MinY;
        MaxX = updated.MaxX;
        MaxY = updated.MaxY;
        MaxSpeed = updated.MaxSpeed;
        PreferredDirection = updated.PreferredDirection;
        CorridorWidth = updated.CorridorWidth;
        AllowTurnaround = updated.AllowTurnaround;
        IsActive = isActive;
        SetUpdated();
    }
}
