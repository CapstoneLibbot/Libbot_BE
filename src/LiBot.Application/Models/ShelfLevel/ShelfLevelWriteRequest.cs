namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelWriteRequest(Guid ShelfId, int LevelNumber, string? Name = null, string Status = "Active")
{
    public Guid ShelfId { get; } = ShelfId;
    public int LevelNumber { get; } = LevelNumber;
    public string? Name { get; } = Name;
    public string Status { get; } = Status;
}
