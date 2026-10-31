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
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double RotationAngle { get; set; }
    public bool IsActive { get; set; } = true;
    public override ZoneEntity ToEntity() => new(MapId, Name, ZoneType, MinX, MinY, MaxX, MaxY,
        MaxSpeed, Description, Width, Height, RotationAngle);
}
