using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Floor : Entity
{
    public string Name { get; private set; }
    public int FloorNumber { get; private set; }
    public string? Description { get; private set; }

    public Floor(string name, int floorNumber, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name is required.", nameof(name));

        Name = name.Trim();
        FloorNumber = floorNumber;
        Description = description?.Trim();
    }

    public void Update(string name, int floorNumber)
    {
        var updated = new Floor(name, floorNumber, Description);
        Name = updated.Name;
        FloorNumber = updated.FloorNumber;
        Description = updated.Description;
        SetUpdated();
    }
}
