using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class LibraryMap : Entity
{
    public Guid? FloorId { get; private set; }
    public string Name { get; private set; }
    public string Version { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string MapData { get; private set; }
    public string Status { get; private set; }

    public LibraryMap(string name, string version, int width, int height, string mapData,
        Guid? floorId = null, string status = "Active")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Map name is required.", nameof(name));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (mapData is null)
            throw new ArgumentNullException(nameof(mapData));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Map status is required.", nameof(status));

        FloorId = floorId;
        Name = name.Trim();
        Version = version?.Trim() ?? string.Empty;
        Width = width;
        Height = height;
        MapData = mapData;
        Status = status.Trim();
    }

    public void Update(string name, string version, int width, int height, string mapData,
        Guid? floorId = null, string status = "Active")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Map name is required.", nameof(name));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (mapData is null)
            throw new ArgumentNullException(nameof(mapData));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Map status is required.", nameof(status));

        FloorId = floorId;
        Name = name.Trim();
        Version = version?.Trim() ?? string.Empty;
        Width = width;
        Height = height;
        MapData = mapData;
        Status = status.Trim();
        SetUpdated();
    }

    public void AssignFloor(Guid? floorId) => FloorId = floorId;
}
