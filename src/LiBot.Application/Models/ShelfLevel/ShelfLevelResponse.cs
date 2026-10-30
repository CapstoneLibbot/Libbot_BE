namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelResponse(Guid Id, Guid ShelfId, int LevelNumber, DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc)
{
    public Guid Id { get; } = Id;
    public Guid ShelfId { get; } = ShelfId;
    public int LevelNumber { get; } = LevelNumber;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
}
