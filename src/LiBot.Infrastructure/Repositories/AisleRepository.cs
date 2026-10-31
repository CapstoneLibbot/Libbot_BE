using LiBot.Application.Interfaces;
using LiBot.Application.Models.Aisle;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class AisleRepository(LiBotDbContext db) : IAisleRepository
{
    public async Task<(IReadOnlyList<AisleResponse> Items, int TotalCount)> SearchAsync(AisleQuery query, CancellationToken cancellationToken)
    {
        var source = db.Aisles.AsNoTracking().AsQueryable();
        if (query.FloorId is Guid floorId) source = source.Where(item => item.FloorId == floorId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(item => EF.Functions.Like(item.Code, $"%{search}%")
                || EF.Functions.Like(item.Name, $"%{search}%")
                || (item.Description != null && EF.Functions.Like(item.Description, $"%{search}%")));
        }

        var total = await source.CountAsync(cancellationToken);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "code" => descending ? source.OrderByDescending(item => item.Code) : source.OrderBy(item => item.Code),
            "createdatutc" or "created" => descending ? source.OrderByDescending(item => item.CreatedAtUtc) : source.OrderBy(item => item.CreatedAtUtc),
            _ => descending ? source.OrderByDescending(item => item.Name) : source.OrderBy(item => item.Name)
        };
        var entities = await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (await ProjectAsync(entities, query.Expand, cancellationToken), total);
    }

    public async Task<AisleResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Aisles.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : (await ProjectAsync([entity], expand, cancellationToken)).Single();
    }

    public Task<bool> FloorExistsAsync(Guid id, CancellationToken cancellationToken) => db.Floors.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<AisleResponse> CreateAsync(AisleWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        var entity = new Aisle(request.FloorId, request.Code, request.Name, request.Description);
        db.Aisles.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, expand, cancellationToken))!;
    }

    public async Task<AisleResponse?> UpdateAsync(Guid id, AisleWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Aisles.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.FloorId, request.Code, request.Name, request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, expand, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Aisles.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return false;
        db.Aisles.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<AisleResponse>> ProjectAsync(IReadOnlyList<Aisle> entities, string? expand, CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var floorIds = entities.Select(item => item.FloorId).Distinct().ToArray();
        var aisleIds = entities.Select(item => item.Id).ToArray();
        var floors = expansions.Contains("floor")
            ? await db.Floors.AsNoTracking().Where(item => floorIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken)
            : [];
        var shelves = expansions.Contains("shelves")
            ? await db.Shelves.AsNoTracking().Where(item => aisleIds.Contains(item.AisleId)).ToListAsync(cancellationToken)
            : [];
        return entities.Select(item => new AisleResponse(
            item.Id, item.FloorId, item.Code, item.Name, item.Description,
            item.CreatedAtUtc, item.UpdatedAtUtc,
            floors.TryGetValue(item.FloorId, out var floor) ? new(floor.Id, floor.Name, floor.FloorNumber) : null,
            expansions.Contains("shelves") ? shelves.Where(shelf => shelf.AisleId == item.Id).Select(shelf => new AisleShelfResponse(shelf.Id, shelf.Code, shelf.Name)).ToList() : null
        )).ToList();
    }
}
