using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class ShelfLevel : Entity
{
    public Guid ShelfId { get; private set; }
    public string Name { get; private set; }
    public int LevelNumber { get; private set; }
    public string Status { get; private set; } = "Active";

    public ShelfLevel(Guid shelfId, int levelNumber, string? name = null, string status = "Active")
    {
        if (shelfId == Guid.Empty)
            throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(levelNumber));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Shelf level status is required.", nameof(status));

        ShelfId = shelfId;
        Name = string.IsNullOrWhiteSpace(name) ? $"Level {levelNumber}" : name.Trim();
        LevelNumber = levelNumber;
        Status = status.Trim();
    }

    public void Update(Guid shelfId, int levelNumber, string? name = null, string status = "Active")
    {
        if (shelfId == Guid.Empty) throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0) throw new ArgumentOutOfRangeException(nameof(levelNumber));
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Shelf level status is required.", nameof(status));
        ShelfId = shelfId;
        Name = string.IsNullOrWhiteSpace(name) ? $"Level {levelNumber}" : name.Trim();
        LevelNumber = levelNumber;
        Status = status.Trim();
        SetUpdated();
    }
}
