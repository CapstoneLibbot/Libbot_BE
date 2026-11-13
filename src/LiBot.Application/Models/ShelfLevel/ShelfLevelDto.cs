using LiBot.Application.Models;
using ShelfLevelEntity = LiBot.Domain.Library.ShelfLevel;

namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelDto : LibraryResourceDto<ShelfLevelEntity>
{
    public Guid ShelfId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LevelNumber { get; set; }
    public string Status { get; set; } = "Active";
    public override ShelfLevelEntity ToEntity() => new(ShelfId, LevelNumber, Name, Status);
}
