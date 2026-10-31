using LiBot.Application.Models.Endpoint;

namespace LiBot.Application.Interfaces;

public interface IEndpointService
{
    Task<EndpointListResponse> GetAllAsync(EndpointQuery query, CancellationToken cancellationToken);
    Task<EndpointResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<EndpointResponse> CreateAsync(EndpointWriteRequest request, CancellationToken cancellationToken);
    Task<EndpointResponse?> UpdateAsync(Guid id, EndpointWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
