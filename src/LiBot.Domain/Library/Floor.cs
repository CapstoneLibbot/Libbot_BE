using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Floor : Entity
{
    public string Name { get; private set; }
    public int FloorNumber { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; } = "Active";

    public Floor(string name, int floorNumber, string? description = null, string status = "Active")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Floor status is required.", nameof(status));

        Name = name.Trim();
        FloorNumber = floorNumber;
        Description = description?.Trim();
        Status = status.Trim();
    }

    public void Update(string name, int floorNumber, string? description = null, string status = "Active")
    {
        var updated = new Floor(name, floorNumber, description, status);
        Name = updated.Name;
        FloorNumber = updated.FloorNumber;
        Description = updated.Description;
        Status = updated.Status;
        SetUpdated();
    }
}
