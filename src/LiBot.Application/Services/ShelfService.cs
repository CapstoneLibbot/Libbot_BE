using LiBot.Application.Interfaces;
using LiBot.Application.Models.Shelf;
using LiBot.Application.Models.ShelfLevel;

namespace LiBot.Application.Services;

public class ShelfService(IShelfRepository repository) : IShelfService
{
    public async Task<ShelfListResponse> GetAllAsync(ShelfQuery query, CancellationToken cancellationToken)
    {
        var normalized = new ShelfQuery(Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 100),
            query.Search, query.AisleId, query.SortBy, query.SortOrder, query.Expand);
        var (items, total) = await repository.SearchAsync(normalized, cancellationToken);
        return new ShelfListResponse(items, normalized.Page, normalized.PageSize, total,
            (int)Math.Ceiling(total / (double)normalized.PageSize), normalized.SortBy.ToLowerInvariant(), normalized.SortOrder.ToLowerInvariant());
    }

    public Task<ShelfResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public async Task<ShelfResponse> CreateAsync(ShelfWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        await ValidateShelfAsync(request, cancellationToken);
        return await repository.CreateAsync(request, expand, cancellationToken);
    }

    public async Task<ShelfResponse?> UpdateAsync(Guid id, ShelfWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        await ValidateShelfAsync(request, cancellationToken);
        return await repository.UpdateAsync(id, request, expand, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => repository.DeleteAsync(id, cancellationToken);

    public async Task<ShelfLevelListResponse> GetLevelsAsync(ShelfLevelQuery query, CancellationToken cancellationToken)
    {
        var normalized = new ShelfLevelQuery(Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 100),
            query.ShelfId, query.SortBy, query.SortOrder);
        var (items, total) = await repository.SearchLevelsAsync(normalized, cancellationToken);
        return new ShelfLevelListResponse(items, normalized.Page, normalized.PageSize, total,
            (int)Math.Ceiling(total / (double)normalized.PageSize), normalized.SortBy.ToLowerInvariant(), normalized.SortOrder.ToLowerInvariant());
    }

    public Task<ShelfLevelResponse?> GetLevelByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetLevelByIdAsync(id, cancellationToken);

    public async Task<ShelfLevelResponse> CreateLevelAsync(ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateLevelAsync(request, cancellationToken);
        return await repository.CreateLevelAsync(request, cancellationToken);
    }

    public async Task<ShelfLevelResponse?> UpdateLevelAsync(Guid id, ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        await ValidateLevelAsync(request, cancellationToken);
        return await repository.UpdateLevelAsync(id, request, cancellationToken);
    }

    public Task<bool> DeleteLevelAsync(Guid id, CancellationToken cancellationToken) => repository.DeleteLevelAsync(id, cancellationToken);

    private async Task ValidateShelfAsync(ShelfWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.AisleId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("aisleId, code and name are required.");
        if (!await repository.AisleExistsAsync(request.AisleId, cancellationToken)) throw new KeyNotFoundException("Aisle was not found.");
    }

    private async Task ValidateLevelAsync(ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.ShelfId == Guid.Empty || request.LevelNumber <= 0) throw new ArgumentException("shelfId and a positive levelNumber are required.");
        if (!await repository.ShelfExistsAsync(request.ShelfId, cancellationToken)) throw new KeyNotFoundException("Shelf was not found.");
    }
}
