using LiBot.Application.Interfaces;
using LiBot.Application.Models.Shelf;
using LiBot.Application.Models.ShelfLevel;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class ShelfRepository(LiBotDbContext db) : IShelfRepository
{
    public async Task<(IReadOnlyList<ShelfResponse> Items, int TotalCount)> SearchAsync(ShelfQuery query, CancellationToken cancellationToken)
    {
        var source = db.Shelves.AsNoTracking().AsQueryable();
        if (query.AisleId is Guid aisleId) source = source.Where(item => item.AisleId == aisleId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(item => EF.Functions.Like(item.Name, $"%{search}%")
                || EF.Functions.Like(item.Status, $"%{search}%"));
        }
        var total = await source.CountAsync(cancellationToken);
        var descending = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "status" => descending ? source.OrderByDescending(item => item.Status) : source.OrderBy(item => item.Status),
            "code" or "created" or "createdatutc" => descending ? source.OrderByDescending(item => item.Name) : source.OrderBy(item => item.Name),
            _ => descending ? source.OrderByDescending(item => item.Name) : source.OrderBy(item => item.Name)
        };
        var entities = await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (await ProjectAsync(entities, query.Expand, cancellationToken), total);
    }

    public async Task<ShelfResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Shelves.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : (await ProjectAsync([entity], expand, cancellationToken)).Single();
    }

    public Task<bool> AisleExistsAsync(Guid id, CancellationToken cancellationToken) => db.Aisles.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<ShelfResponse> CreateAsync(ShelfWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        var entity = new Shelf(request.AisleId, request.StartEndpointId, request.EndEndpointId,
            request.Name, request.Description, request.Status);
        db.Shelves.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, expand, cancellationToken))!;
    }

    public async Task<ShelfResponse?> UpdateAsync(Guid id, ShelfWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Shelves.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.AisleId, request.StartEndpointId, request.EndEndpointId,
            request.Name, request.Description, request.Status);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, expand, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Shelves.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return false;
        db.Shelves.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(IReadOnlyList<ShelfLevelResponse> Items, int TotalCount)> SearchLevelsAsync(ShelfLevelQuery query, CancellationToken cancellationToken)
    {
        var source = db.ShelfLevels.AsNoTracking().AsQueryable();
        if (query.ShelfId is Guid shelfId) source = source.Where(item => item.ShelfId == shelfId);
        var total = await source.CountAsync(cancellationToken);
        var descending = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "status" => descending ? source.OrderByDescending(item => item.Status) : source.OrderBy(item => item.Status),
            _ => descending ? source.OrderByDescending(item => item.LevelNumber) : source.OrderBy(item => item.LevelNumber)
        };
        var entities = await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (entities.Select(ToLevelResponse).ToList(), total);
    }

    public async Task<ShelfLevelResponse?> GetLevelByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.ShelfLevels.AsNoTracking().FirstOrDefaultAsync(value => value.Id == id, cancellationToken);
        return item is null ? null : ToLevelResponse(item);
    }

    public Task<bool> ShelfExistsAsync(Guid id, CancellationToken cancellationToken) => db.Shelves.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<ShelfLevelResponse> CreateLevelAsync(ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        var entity = new ShelfLevel(request.ShelfId, request.LevelNumber, request.Name, request.Status);
        db.ShelfLevels.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetLevelByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ShelfLevelResponse?> UpdateLevelAsync(Guid id, ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.ShelfLevels.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.ShelfId, request.LevelNumber, request.Name, request.Status);
        await db.SaveChangesAsync(cancellationToken);
        return await GetLevelByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteLevelAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ShelfLevels.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return false;
        db.ShelfLevels.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<ShelfResponse>> ProjectAsync(IReadOnlyList<Shelf> entities, string? expand, CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var aisleIds = entities.Select(item => item.AisleId).Distinct().ToArray();
        var shelfIds = entities.Select(item => item.Id).ToArray();
        var aisles = expansions.Contains("aisle")
            ? await db.Aisles.AsNoTracking().Where(item => aisleIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken)
            : [];
        var levels = expansions.Contains("levels")
            ? await db.ShelfLevels.AsNoTracking().Where(item => shelfIds.Contains(item.ShelfId)).ToListAsync(cancellationToken)
            : [];
        return entities.Select(item => new ShelfResponse(item.Id, item.AisleId, item.StartEndpointId, item.EndEndpointId,
            item.Name, item.Description, item.Status,
            aisles.TryGetValue(item.AisleId, out var aisle) ? new ShelfAisleResponse(aisle.Id, aisle.Name, aisle.FloorId) : null,
            expansions.Contains("levels") ? levels.Where(level => level.ShelfId == item.Id).OrderBy(level => level.LevelNumber)
                .Select(ToLevelResponse).ToList() : null)).ToList();
    }

    private static ShelfLevelResponse ToLevelResponse(ShelfLevel item) =>
        new(item.Id, item.ShelfId, item.Name, item.LevelNumber, item.Status);
}
