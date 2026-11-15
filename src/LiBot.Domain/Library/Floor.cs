using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Floor : Entity
{
    public string Name { get; private set; }
    public int FloorNumber { get; private set; }
    public string? Description { get; private set; }
    public FloorStatus Status { get; private set; } = FloorStatus.ACTIVE;

    public Floor(string name, int floorNumber, string? description = null, FloorStatus status = FloorStatus.ACTIVE)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name is required.", nameof(name));

        Name = name.Trim();
        FloorNumber = floorNumber;
        Description = description?.Trim();
        Status = status;
    }

    public Floor(string name, int floorNumber, string? description, string status)
        : this(name, floorNumber, description, ParseStatus(status))
    {
    }

    public void Update(string name, int floorNumber, string? description = null, FloorStatus status = FloorStatus.ACTIVE)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name is required.", nameof(name));

        Name = name.Trim();
        FloorNumber = floorNumber;
        Description = description?.Trim();
        Status = status;
        SetUpdated();
    }

    public void Update(string name, int floorNumber, string? description, string status)
    {
        Update(name, floorNumber, description, ParseStatus(status));
    }

    public void SetStatus(FloorStatus status)
    {
        Status = status;
        SetUpdated();
    }

    private static FloorStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return FloorStatus.ACTIVE;
        if (Enum.TryParse<FloorStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid floor status '{status}'.", nameof(status));
    }
}
