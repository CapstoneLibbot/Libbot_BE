using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Aisle : Entity
{
    public Guid FloorId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; } = "Active";

    public Aisle(Guid floorId, string name, string? description = null, string status = "Active")
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Aisle name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Aisle status is required.", nameof(status));

        FloorId = floorId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status.Trim();
    }

    public void Update(Guid floorId, string name, string? description, string status)
    {
        var updated = new Aisle(floorId, name, description, status);
        FloorId = updated.FloorId;
        Name = updated.Name;
        Description = updated.Description;
        Status = updated.Status;
        SetUpdated();
    }
}
