using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Floor : Entity
{
    public string Name { get; private set; }
    public int FloorNumber { get; private set; }

    public Floor(string name, int floorNumber)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Floor name is required.", nameof(name));

        Name = name.Trim();
        FloorNumber = floorNumber;
    }
}