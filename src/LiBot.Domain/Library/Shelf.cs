using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Shelf : Entity
{
    public Guid AisleId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }

    public Shelf(Guid aisleId, string code, string name)
    {
        if (aisleId == Guid.Empty)
            throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Shelf code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shelf name is required.", nameof(name));

        AisleId = aisleId;
        Code = code.Trim();
        Name = name.Trim();
    }

    public void Update(Guid aisleId, string code, string name)
    {
        if (aisleId == Guid.Empty) throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Shelf code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Shelf name is required.", nameof(name));
        AisleId = aisleId;
        Code = code.Trim();
        Name = name.Trim();
        SetUpdated();
    }
}
