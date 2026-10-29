using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Area : Entity
{
    public Guid FloorId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public Area(Guid floorId, string name, string? description = null)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Area name is required.", nameof(name));

        FloorId = floorId;
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Update(Guid floorId, string name, string? description = null)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Area name is required.", nameof(name));

        FloorId = floorId;
        Name = name.Trim();
        Description = description?.Trim();
        SetUpdated();
    }
}
