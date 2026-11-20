namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelResponse(Guid Id, Guid ShelfId, string Name, int LevelNumber, string Status)
{
    public Guid Id { get; } = Id;
    public Guid ShelfId { get; } = ShelfId;
    public string Name { get; } = Name;
    public int LevelNumber { get; } = LevelNumber;
    public string Status { get; } = Status;
}
