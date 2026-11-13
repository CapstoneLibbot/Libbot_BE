using LiBot.Application.Interfaces;
using LiBot.Application.Models.Floor;

namespace LiBot.Application.Services;

public class FloorService(IFloorRepository repository) : IFloorService
{
    public async Task<FloorListResponse> GetAllAsync(FloorQuery query, CancellationToken cancellationToken)
    {
        var normalized = new FloorQuery(Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 100),
            query.Search, query.SortBy, query.SortOrder, query.Expand);
        var (items, total) = await repository.SearchAsync(normalized, cancellationToken);
        return new FloorListResponse(items, normalized.Page, normalized.PageSize, total,
            (int)Math.Ceiling(total / (double)normalized.PageSize), normalized.SortBy.ToLowerInvariant(), normalized.SortOrder.ToLowerInvariant());
    }

    public Task<FloorResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public Task<FloorResponse> CreateAsync(FloorWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        Validate(request);
        return repository.CreateAsync(request, expand, cancellationToken);
    }

    public Task<FloorResponse?> UpdateAsync(Guid id, FloorWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        Validate(request);
        return repository.UpdateAsync(id, request, expand, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => repository.DeleteAsync(id, cancellationToken);

    private static void Validate(FloorWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Status))
            throw new ArgumentException("name and status are required.");
    }
}
