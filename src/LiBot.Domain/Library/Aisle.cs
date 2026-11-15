using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Aisle : Entity
{
    public Guid FloorId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public AisleStatus Status { get; private set; } = AisleStatus.ACTIVE;

    public Aisle(Guid floorId, string name, string? description = null, AisleStatus status = AisleStatus.ACTIVE)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Aisle name is required.", nameof(name));

        FloorId = floorId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
    }

    public Aisle(Guid floorId, string name, string? description, string status)
        : this(floorId, name, description, ParseStatus(status))
    {
    }

    public void Update(Guid floorId, string name, string? description = null, AisleStatus status = AisleStatus.ACTIVE)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Aisle name is required.", nameof(name));

        FloorId = floorId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
        SetUpdated();
    }

    public void Update(Guid floorId, string name, string? description, string status)
    {
        Update(floorId, name, description, ParseStatus(status));
    }

    public void SetStatus(AisleStatus status)
    {
        Status = status;
        SetUpdated();
    }

    private static AisleStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return AisleStatus.ACTIVE;
        if (Enum.TryParse<AisleStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid aisle status '{status}'.", nameof(status));
    }
}
