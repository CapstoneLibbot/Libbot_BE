using LiBot.Application.Interfaces;
using LiBot.Application.Models.Endpoint;

namespace LiBot.Application.Services;

public class EndpointService(IEndpointRepository repository) : IEndpointService
{
    public async Task<EndpointListResponse> GetAllAsync(EndpointQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalized = new EndpointQuery(
            page, pageSize, query.Search, query.MapId, query.IsActive, query.SortBy, query.SortOrder, query.Expand);
        var (items, totalCount) = await repository.SearchAsync(normalized, cancellationToken);
        return new EndpointListResponse(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            normalized.SortBy.ToLowerInvariant(),
            normalized.SortOrder.ToLowerInvariant());
    }

    public Task<EndpointResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public async Task<EndpointResponse> CreateAsync(EndpointWriteRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await ValidateMapAsync(request.MapId, cancellationToken);
        return await repository.CreateAsync(request, cancellationToken);
    }

    public async Task<EndpointResponse?> UpdateAsync(Guid id, EndpointWriteRequest request, CancellationToken cancellationToken)
    {
        ValidateRequest(request);
        await ValidateMapAsync(request.MapId, cancellationToken);
        return await repository.UpdateAsync(id, request, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    private static void ValidateRequest(EndpointWriteRequest request)
    {
        if (request.MapId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("mapId and name are required.");
        if (!double.IsFinite(request.PositionX) || !double.IsFinite(request.PositionY) || !double.IsFinite(request.Theta))
            throw new ArgumentException("positionX, positionY and theta must be finite numbers.");
    }

    private async Task ValidateMapAsync(Guid mapId, CancellationToken cancellationToken)
    {
        if (!await repository.MapExistsAsync(mapId, cancellationToken))
            throw new KeyNotFoundException("Map was not found.");
    }
}
