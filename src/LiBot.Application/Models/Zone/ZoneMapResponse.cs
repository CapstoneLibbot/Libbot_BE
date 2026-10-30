namespace LiBot.Application.Models.Zone;

public class ZoneMapResponse(Guid Id, string Name, string Version, int Width, int Height)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public string Version { get; } = Version;
    public int Width { get; } = Width;
    public int Height { get; } = Height;
}
