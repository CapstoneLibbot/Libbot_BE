using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Shelf : Entity
{
    public Guid AisleId { get; private set; }
    public Guid? EndpointId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }

    public Shelf(Guid aisleId, string code, string name, Guid? endpointId = null)
    {
        if (aisleId == Guid.Empty)
            throw new ArgumentException("Aisle id is required.", nameof(aisleId));
        if (endpointId == Guid.Empty)
            throw new ArgumentException("Endpoint id must be a non-empty GUID when provided.", nameof(endpointId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Shelf code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Shelf name is required.", nameof(name));

        AisleId = aisleId;
        EndpointId = endpointId;
        Code = code.Trim();
        Name = name.Trim();
    }
}
