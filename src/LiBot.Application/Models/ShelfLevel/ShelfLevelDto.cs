using LiBot.Application.Models;
using ShelfLevelEntity = LiBot.Domain.Library.ShelfLevel;

namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelDto : LibraryResourceDto<ShelfLevelEntity>
{
    public Guid ShelfId { get; set; }
    public int LevelNumber { get; set; }
    public override ShelfLevelEntity ToEntity() => new(ShelfId, LevelNumber);
}
