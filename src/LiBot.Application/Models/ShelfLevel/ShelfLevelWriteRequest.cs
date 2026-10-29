namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelWriteRequest(Guid ShelfId, int LevelNumber)
{
    public Guid ShelfId { get; } = ShelfId;
    public int LevelNumber { get; } = LevelNumber;
}
