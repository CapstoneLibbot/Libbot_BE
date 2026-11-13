using LiBot.Application.Models;
using ZoneEntity = LiBot.Domain.Library.Zone;

namespace LiBot.Application.Models.Zone;

public class ZoneDto : LibraryResourceDto<ZoneEntity>
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ZoneType { get; set; } = string.Empty;
    public double MaxSpeed { get; set; }
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public bool IsActive { get; set; } = true;
    public override ZoneEntity ToEntity() => new(MapId, ZoneType, MinX, MinY, MaxX, MaxY,
        MaxSpeed, name: Name, description: Description);
}
