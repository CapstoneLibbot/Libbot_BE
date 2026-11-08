namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelResponse(Guid Id, Guid ShelfId, int LevelNumber, DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc, string? Name = null, string Status = "Active")
{
    public Guid Id { get; } = Id;
    public Guid ShelfId { get; } = ShelfId;
    public int LevelNumber { get; } = LevelNumber;
    public string? Name { get; } = Name;
    public string Status { get; } = Status;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
}
