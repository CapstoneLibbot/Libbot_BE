using LiBot.Domain.Common;
using LiBot.Domain.Enums;

namespace LiBot.Domain.Library;

public class Floor : Entity
{
    public string Name { get; private set; }
    public int FloorNumber { get; private set; }
    public string? Description { get; set; }
    public FloorStatus Status { get; set; } = LiBot.Domain.Enums.FloorStatus.Active;

    private Floor()
    {
        Name = string.Empty;
    }

    public Floor(string name, int floorNumber, string? description = null, FloorStatus status = FloorStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name is required.", nameof(name));

        Name = name.Trim();
        FloorNumber = floorNumber;
        Description = description?.Trim();
        Status = status;
    }

    public void Update(string name, int floorNumber, string? description, FloorStatus? status)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Floor name is required.", nameof(name));
        Name = name.Trim();
        FloorNumber = floorNumber;
        Description = description?.Trim();
        Status = status ?? LiBot.Domain.Enums.FloorStatus.Active;
        SetUpdated();
    }
}
