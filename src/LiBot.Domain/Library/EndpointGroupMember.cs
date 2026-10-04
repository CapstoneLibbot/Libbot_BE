using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class EndpointGroupMember : Entity
{
    public Guid EndpointGroupId { get; private set; }
    public Guid EndpointId { get; private set; }

    public EndpointGroupMember(Guid endpointGroupId, Guid endpointId)
    {
        if (endpointGroupId == Guid.Empty)
            throw new ArgumentException("Endpoint group id is required.", nameof(endpointGroupId));
        if (endpointId == Guid.Empty)
            throw new ArgumentException("Endpoint id is required.", nameof(endpointId));

        EndpointGroupId = endpointGroupId;
        EndpointId = endpointId;
    }
}