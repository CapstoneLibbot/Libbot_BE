namespace LiBot.Application.Models.ShelfLevel;

public class ShelfLevelResponse
{
    public ShelfLevelResponse(Guid id, Guid shelfId, string name, int levelNumber, string status)
        : this(id, shelfId, levelNumber, default, null, name, status) { }

    public ShelfLevelResponse(Guid id, Guid shelfId, int levelNumber, DateTime createdAtUtc,
        DateTime? updatedAtUtc, string? name = null, string? status = null)
    {
        Id = id; ShelfId = shelfId; LevelNumber = levelNumber; CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc; Name = name ?? $"Level {levelNumber}"; Status = status ?? "Active";
    }

    public Guid Id { get; }
    public Guid ShelfId { get; }
    public string Name { get; }
    public int LevelNumber { get; }
    public string Status { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
}
