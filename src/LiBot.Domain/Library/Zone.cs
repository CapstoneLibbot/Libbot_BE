using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Zone : Entity
{
    public static readonly IReadOnlySet<string> SupportedTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "EXCLUSION",
        "SPEED_LIMIT",
        "PREFERRED_DIRECTION",
        "NARROW_CORRIDOR"
    };

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
        var normalizedType = zoneType.Trim().ToUpperInvariant();
        if (!SupportedTypes.Contains(normalizedType))
            throw new ArgumentException($"Unsupported zone type '{zoneType}'.", nameof(zoneType));
        if (maxSpeed < 0)
            throw new ArgumentOutOfRangeException(nameof(maxSpeed));
        if (corridorWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(corridorWidth));
        if (normalizedType == "SPEED_LIMIT" && maxSpeed <= 0)
            throw new ArgumentException("SPEED_LIMIT requires maxSpeed greater than zero.", nameof(maxSpeed));
        if (normalizedType == "PREFERRED_DIRECTION" && string.IsNullOrWhiteSpace(preferredDirection))
            throw new ArgumentException("PREFERRED_DIRECTION requires preferredDirection.", nameof(preferredDirection));
        if (normalizedType == "NARROW_CORRIDOR" && corridorWidth is null or <= 0)
            throw new ArgumentException("NARROW_CORRIDOR requires corridorWidth greater than zero.", nameof(corridorWidth));

        MapId = mapId;
        ZoneType = normalizedType;
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
