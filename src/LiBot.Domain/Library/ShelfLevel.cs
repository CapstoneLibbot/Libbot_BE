using LiBot.Domain.Common;
using LiBot.Domain.Enums;

namespace LiBot.Domain.Library;

public class ShelfLevel : Entity
{
    public Guid ShelfId { get; private set; }
    public string? Name { get; private set; }
    public int LevelNumber { get; private set; }
    public ShelfStatus Status { get; private set; } = ShelfStatus.Active;

    public ShelfLevel(Guid shelfId, int levelNumber, string? name = null, ShelfStatus status = ShelfStatus.Active)
    {
        if (shelfId == Guid.Empty)
            throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(levelNumber));

        ShelfId = shelfId;
        LevelNumber = levelNumber;
        Name = name?.Trim();
        Status = status;
    }

    public void Update(Guid shelfId, int levelNumber, string? name = null, ShelfStatus? status = null)
    {
        if (shelfId == Guid.Empty) throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0) throw new ArgumentOutOfRangeException(nameof(levelNumber));
        ShelfId = shelfId;
        LevelNumber = levelNumber;
        Name = name?.Trim();
        Status = status ?? Status;
        SetUpdated();
    }
}
