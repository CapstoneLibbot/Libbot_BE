using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class LibraryMap : Entity
{
    public Guid FloorId { get; private set; }
    public string Name { get; private set; }
    public string Version { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string MapData { get; private set; }
    public MapStatus Status { get; private set; } = MapStatus.Active;

    public LibraryMap(string name, string version, int width, int height, string mapData,
        Guid floorId, MapStatus status = MapStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Map name is required.", nameof(name));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (mapData is null)
            throw new ArgumentNullException(nameof(mapData));
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));

        FloorId = floorId;
        Name = name.Trim();
        Version = version?.Trim() ?? string.Empty;
        Width = width;
        Height = height;
        MapData = mapData;
        Status = status;
    }

    public LibraryMap(string name, string version, int width, int height, string mapData,
        Guid floorId, string status)
        : this(name, version, width, height, mapData, floorId, ParseStatus(status))
    {
    }

    public void Update(string name, string version, int width, int height, string mapData,
        Guid floorId, MapStatus status = MapStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Map name is required.", nameof(name));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (mapData is null)
            throw new ArgumentNullException(nameof(mapData));
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));

        FloorId = floorId;
        Name = name.Trim();
        Version = version?.Trim() ?? string.Empty;
        Width = width;
        Height = height;
        MapData = mapData;
        Status = status;
        SetUpdated();
    }

    public void Update(string name, string version, int width, int height, string mapData,
        Guid floorId, string status)
    {
        Update(name, version, width, height, mapData, floorId, ParseStatus(status));
    }

    public void AssignFloor(Guid floorId)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        FloorId = floorId;
    }

    public void SetStatus(MapStatus status)
    {
        Status = status;
        SetUpdated();
    }

    private static MapStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return MapStatus.Active;
        if (Enum.TryParse<MapStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid map status '{status}'.", nameof(status));
    }
}
