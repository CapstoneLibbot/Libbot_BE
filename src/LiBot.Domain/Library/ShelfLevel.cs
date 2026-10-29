using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class ShelfLevel : Entity
{
    public Guid ShelfId { get; private set; }
    public int LevelNumber { get; private set; }

    public ShelfLevel(Guid shelfId, int levelNumber)
    {
        if (shelfId == Guid.Empty)
            throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(levelNumber));

        ShelfId = shelfId;
        LevelNumber = levelNumber;
    }

    public void Update(Guid shelfId, int levelNumber)
    {
        if (shelfId == Guid.Empty) throw new ArgumentException("Shelf id is required.", nameof(shelfId));
        if (levelNumber <= 0) throw new ArgumentOutOfRangeException(nameof(levelNumber));
        ShelfId = shelfId;
        LevelNumber = levelNumber;
        SetUpdated();
    }
}
