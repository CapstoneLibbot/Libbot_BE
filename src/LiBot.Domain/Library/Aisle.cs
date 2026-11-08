using LiBot.Domain.Common;
using LiBot.Domain.Enums;

namespace LiBot.Domain.Library;

public class Aisle : Entity
{
    public Guid FloorId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AisleStatus Status { get; set; } = LiBot.Domain.Enums.AisleStatus.Active;

    public Aisle(Guid floorId, string name, string? description = null)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Aisle name is required.", nameof(name));

        FloorId = floorId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = AisleStatus.Active;
    }

    public void Update(Guid floorId, string name, string? description, AisleStatus? status)
    {
        if (floorId == Guid.Empty) throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Aisle name is required.", nameof(name));
        FloorId = floorId;
        Status = status ?? LiBot.Domain.Enums.AisleStatus.Active;
        Name = name.Trim();
        Description = description?.Trim();
        SetUpdated();
    }
}
