using LiBot.Application.Interfaces;
using LiBot.Application.Models.Area;

namespace LiBot.Application.Services;

public class AreaService(IAreaRepository repository) : IAreaService
{
    public async Task<AreaListResponse> GetAllAsync(AreaQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalized = new AreaQuery(
            page,
            pageSize,
            query.Search,
            query.FloorId,
            query.SortBy,
            query.SortOrder,
            query.Expand);
        var (items, totalCount) = await repository.SearchAsync(normalized, cancellationToken);
        return new AreaListResponse(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            normalized.SortBy.ToLowerInvariant(),
            normalized.SortOrder.ToLowerInvariant());
    }

    public Task<AreaResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public async Task<AreaResponse> CreateAsync(AreaWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request, cancellationToken);
        return await repository.CreateAsync(request, expand, cancellationToken);
    }

    public async Task<AreaResponse?> UpdateAsync(Guid id, AreaWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(request, cancellationToken);
        return await repository.UpdateAsync(id, request, expand, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    private async Task ValidateReferencesAsync(AreaWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.FloorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("floorId and name are required.");
        if (!await repository.FloorExistsAsync(request.FloorId, cancellationToken))
            throw new KeyNotFoundException("Floor was not found.");
    }
}
