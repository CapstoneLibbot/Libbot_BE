using LiBot.Application.Models;
using ShelfEntity = LiBot.Domain.Library.Shelf;

namespace LiBot.Application.Models.Shelf;

public class ShelfDto : LibraryResourceDto<ShelfEntity>
{
    public Guid AisleId { get; set; }
    public Guid? EndpointId { get; set; }
    public Guid StartEndpointId { get; set; }
    public Guid EndEndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public override ShelfEntity ToEntity() => new(AisleId, Name,
        EndpointId ?? (StartEndpointId == Guid.Empty ? null : StartEndpointId), Description,
        Enum.Parse<LiBot.Domain.Enums.ShelfStatus>(Status, true));
}
