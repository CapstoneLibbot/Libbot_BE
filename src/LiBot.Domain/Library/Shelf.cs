using LiBot.Domain.Common;
using LiBot.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiBot.Domain.Library;

public class Shelf : Entity
{
    public Guid AisleId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ShelfStatus Status { get; private set; } = ShelfStatus.Active;

    [NotMapped]
    public string Code => Name;

    public Shelf(Guid aisleId, string name, Guid? endpointId = null, string? description = null, ShelfStatus status = ShelfStatus.Active)
    {
        if (aisleId == Guid.Empty)
            throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shelf name is required.", nameof(name));

        AisleId = aisleId;
        Name = name.Trim();
        EndpointId = endpointId;
        Description = description?.Trim();
        Status = status;
    }

    // Compatibility overload for older clients; the ERD no longer persists a separate code.
    public Shelf(Guid aisleId, string code, string name)
        : this(aisleId, name)
    {
    }

    public void Update(Guid aisleId, string name, Guid? endpointId = null, string? description = null, ShelfStatus? status = null)
    {
        if (aisleId == Guid.Empty) throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Shelf name is required.", nameof(name));
        AisleId = aisleId;
        Name = name.Trim();
        EndpointId = endpointId;
        Description = description?.Trim();
        Status = status ?? Status;
        SetUpdated();
    }

    public void Update(Guid aisleId, string code, string name) => Update(aisleId, name);
}
