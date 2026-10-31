using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Area : Entity
{
    public Guid FloorId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public Area(Guid floorId, string name, string? description = null, Guid? endpointId = null)
    public Area(Guid floorId, string name, string? description = null, Guid? endpointId = null)
    {
        if (floorId == Guid.Empty)
            throw new ArgumentException("Floor id is required.", nameof(floorId));
        if (endpointId == Guid.Empty)
            throw new ArgumentException("Endpoint id must be a non-empty GUID when provided.", nameof(endpointId));
        if (endpointId == Guid.Empty)
            throw new ArgumentException("Endpoint id must be a non-empty GUID when provided.", nameof(endpointId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Area name is required.", nameof(name));

        FloorId = floorId;
        EndpointId = endpointId;
        EndpointId = endpointId;
        Name = name.Trim();
        Description = description?.Trim();
    }
}
