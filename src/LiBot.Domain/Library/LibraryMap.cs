using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class LibraryMap : Entity
{
    public string Name { get; private set; }
    public string Version { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string MapData { get; private set; }

    public LibraryMap(string name, string version, int width, int height, string mapData)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Map name is required.", nameof(name));
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));
        if (mapData is null)
            throw new ArgumentNullException(nameof(mapData));

        Name = name.Trim();
        Version = version?.Trim() ?? string.Empty;
        Width = width;
        Height = height;
        MapData = mapData;
    }
}