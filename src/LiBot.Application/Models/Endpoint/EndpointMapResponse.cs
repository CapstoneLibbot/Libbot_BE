namespace LiBot.Application.Models.Endpoint;

public class EndpointMapResponse(
    Guid Id,
    string Name,
    string Version,
    int Width,
    int Height,
    string MapData)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public string Version { get; } = Version;
    public int Width { get; } = Width;
    public int Height { get; } = Height;
    public string MapData { get; } = MapData;
}
