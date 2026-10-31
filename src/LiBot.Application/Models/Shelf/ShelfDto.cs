using LiBot.Application.Models;
using ShelfEntity = LiBot.Domain.Library.Shelf;

namespace LiBot.Application.Models.Shelf;

public class ShelfDto : LibraryResourceDto<ShelfEntity>
{
    public Guid AisleId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public override ShelfEntity ToEntity() => new(AisleId, Code, Name);
}
