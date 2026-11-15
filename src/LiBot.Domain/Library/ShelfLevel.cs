using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class ShelfLevel : Entity
{
    public Guid ShelfId { get; private set; }
    public string Name { get; private set; }
    public int LevelNumber { get; private set; }
    public ShelfLevelStatus Status { get; private set; } = ShelfLevelStatus.ACTIVE;

    public ShelfLevel(Guid shelfId, int levelNumber, string? name = null, ShelfLevelStatus status = ShelfLevelStatus.ACTIVE)
    {
        if (shelfId == Guid.Empty)
            throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(levelNumber));

        ShelfId = shelfId;
        Name = string.IsNullOrWhiteSpace(name) ? $"Level {levelNumber}" : name.Trim();
        LevelNumber = levelNumber;
        Status = status;
    }

    public ShelfLevel(Guid shelfId, int levelNumber, string? name, string status)
        : this(shelfId, levelNumber, name, ParseStatus(status))
    {
    }

    public void Update(Guid shelfId, int levelNumber, string? name = null, ShelfLevelStatus status = ShelfLevelStatus.ACTIVE)
    {
        if (shelfId == Guid.Empty) throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0) throw new ArgumentOutOfRangeException(nameof(levelNumber));
        ShelfId = shelfId;
        Name = string.IsNullOrWhiteSpace(name) ? $"Level {levelNumber}" : name.Trim();
        LevelNumber = levelNumber;
        Status = status;
        SetUpdated();
    }

    public void Update(Guid shelfId, int levelNumber, string? name, string status)
    {
        Update(shelfId, levelNumber, name, ParseStatus(status));
    }

    public void SetStatus(ShelfLevelStatus status)
    {
        Status = status;
        SetUpdated();
    }

    private static ShelfLevelStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return ShelfLevelStatus.ACTIVE;
        if (Enum.TryParse<ShelfLevelStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid shelf level status '{status}'.", nameof(status));
    }
}
