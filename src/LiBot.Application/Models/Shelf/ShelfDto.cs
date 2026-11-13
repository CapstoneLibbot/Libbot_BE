using LiBot.Application.Models;
using ShelfEntity = LiBot.Domain.Library.Shelf;

namespace LiBot.Application.Models.Shelf;

public class ShelfDto : LibraryResourceDto<ShelfEntity>
{
    public Guid AisleId { get; set; }
    public Guid StartEndpointId { get; set; }
    public Guid EndEndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public override ShelfEntity ToEntity() => new(AisleId, StartEndpointId, EndEndpointId, Name, Description, Status);
}
