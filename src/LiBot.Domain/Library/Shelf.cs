using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Shelf : Entity
{
    public Guid AisleId { get; private set; }
    public Guid StartEndpointId { get; private set; }
    public Guid EndEndpointId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; } = "Active";

    public Shelf(Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description = null, string status = "Active")
    {
        if (aisleId == Guid.Empty)
            throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (startEndpointId == Guid.Empty || endEndpointId == Guid.Empty)
            throw new ArgumentException("Shelf endpoints are required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shelf name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Shelf status is required.", nameof(status));

        AisleId = aisleId;
        StartEndpointId = startEndpointId;
        EndEndpointId = endEndpointId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status.Trim();
    }

    public void SetEndpoints(Guid startEndpointId, Guid endEndpointId)
    {
        if (startEndpointId == Guid.Empty || endEndpointId == Guid.Empty)
            throw new ArgumentException("Shelf endpoints are required.");
        StartEndpointId = startEndpointId;
        EndEndpointId = endEndpointId;
    }

    public void Update(Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description, string status)
    {
        var updated = new Shelf(aisleId, startEndpointId, endEndpointId, name, description, status);
        AisleId = updated.AisleId;
        StartEndpointId = updated.StartEndpointId;
        EndEndpointId = updated.EndEndpointId;
        Name = updated.Name;
        Description = updated.Description;
        Status = updated.Status;
        SetUpdated();
    }
}
