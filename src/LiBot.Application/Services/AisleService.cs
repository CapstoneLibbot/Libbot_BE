using LiBot.Application.Interfaces;
using LiBot.Application.Models.Aisle;

namespace LiBot.Application.Services;

public class AisleService(IAisleRepository repository) : IAisleService
{
    public async Task<AisleListResponse> GetAllAsync(AisleQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalized = new AisleQuery(
            page,
            pageSize,
            query.Search,
            query.FloorId,
            query.EndpointId,
            query.SortBy,
            query.SortOrder,
            query.Expand);
        var (items, totalCount) = await repository.SearchAsync(normalized, cancellationToken);
        return new AisleListResponse(items, page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            normalized.SortBy.ToLowerInvariant(), normalized.SortOrder.ToLowerInvariant());
    }

    public Task<AisleResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public async Task<AisleResponse> CreateAsync(AisleWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request, cancellationToken);
        return await repository.CreateAsync(request, expand, cancellationToken);
    }

    public async Task<AisleResponse?> UpdateAsync(Guid id, AisleWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request, cancellationToken);
        return await repository.UpdateAsync(id, request, expand, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => repository.DeleteAsync(id, cancellationToken);

    private async Task ValidateReferencesAsync(AisleWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.FloorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("floorId, code and name are required.");
        if (!await repository.FloorExistsAsync(request.FloorId, cancellationToken))
            throw new KeyNotFoundException("Floor was not found.");
        if (request.EndpointId is Guid endpointId && !await repository.EndpointExistsAsync(endpointId, cancellationToken))
            throw new KeyNotFoundException("Endpoint was not found.");
    }
}
