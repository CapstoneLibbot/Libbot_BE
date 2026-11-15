using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Shelf : Entity
{
    public Guid AisleId { get; private set; }
    public Guid StartEndpointId { get; private set; }
    public Guid EndEndpointId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ShelfStatus Status { get; private set; } = ShelfStatus.ACTIVE;

    public Shelf(Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description = null, ShelfStatus status = ShelfStatus.ACTIVE)
    {
        if (aisleId == Guid.Empty)
            throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (startEndpointId == Guid.Empty || endEndpointId == Guid.Empty)
            throw new ArgumentException("Shelf endpoints are required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shelf name is required.", nameof(name));

        AisleId = aisleId;
        StartEndpointId = startEndpointId;
        EndEndpointId = endEndpointId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
    }

    public Shelf(Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description, string status)
        : this(aisleId, startEndpointId, endEndpointId, name, description, ParseStatus(status))
    {
    }

    public void SetEndpoints(Guid startEndpointId, Guid endEndpointId)
    {
        if (startEndpointId == Guid.Empty || endEndpointId == Guid.Empty)
            throw new ArgumentException("Shelf endpoints are required.");
        StartEndpointId = startEndpointId;
        EndEndpointId = endEndpointId;
        SetUpdated();
    }

    public void Update(Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description = null, ShelfStatus status = ShelfStatus.ACTIVE)
    {
        if (aisleId == Guid.Empty)
            throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (startEndpointId == Guid.Empty || endEndpointId == Guid.Empty)
            throw new ArgumentException("Shelf endpoints are required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shelf name is required.", nameof(name));

        AisleId = aisleId;
        StartEndpointId = startEndpointId;
        EndEndpointId = endEndpointId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
        SetUpdated();
    }

    public void Update(Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description, string status)
    {
        Update(aisleId, startEndpointId, endEndpointId, name, description, ParseStatus(status));
    }

    public void SetStatus(ShelfStatus status)
    {
        Status = status;
        SetUpdated();
    }

    private static ShelfStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return ShelfStatus.ACTIVE;
        if (Enum.TryParse<ShelfStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid shelf status '{status}'.", nameof(status));
    }
}
