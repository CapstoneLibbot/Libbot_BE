using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Aisle : Entity
{
    public Guid FloorId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public Aisle(Guid floorId, string code, string name, string? description = null)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Aisle code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Aisle name is required.", nameof(name));

        FloorId = floorId;
        Code = code.Trim();
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Update(Guid floorId, string code, string name, string? description)
    {
        var updated = new Aisle(floorId, code, name, description);
        FloorId = updated.FloorId;
        Code = updated.Code;
        Name = updated.Name;
        Description = updated.Description;
        SetUpdated();
    }
}
