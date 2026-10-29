using LiBot.Application.Interfaces;
using LiBot.Application.Models.Area;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class AreaRepository(LiBotDbContext db) : IAreaRepository
{
    public async Task<(IReadOnlyList<AreaResponse> Items, int TotalCount)> SearchAsync(
        AreaQuery query,
        CancellationToken cancellationToken)
    {
        var source = db.Areas.AsNoTracking().AsQueryable();
        if (query.FloorId is Guid floorId)
            source = source.Where(item => item.FloorId == floorId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(item => EF.Functions.Like(item.Name, $"%{search}%")
                || (item.Description != null && EF.Functions.Like(item.Description, $"%{search}%")));
        }

        var total = await source.CountAsync(cancellationToken);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "createdatutc" or "created" => descending
                ? source.OrderByDescending(item => item.CreatedAtUtc)
                : source.OrderBy(item => item.CreatedAtUtc),
            _ => descending
                ? source.OrderByDescending(item => item.Name)
                : source.OrderBy(item => item.Name)
        };

        var entities = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return (await ProjectAsync(entities, query.Expand, cancellationToken), total);
    }

    public async Task<AreaResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Areas.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null
            ? null
            : (await ProjectAsync([entity], expand, cancellationToken)).Single();
    }

    public Task<bool> FloorExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Floors.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<AreaResponse> CreateAsync(
        AreaWriteRequest request,
        string? expand,
        CancellationToken cancellationToken)
    {
        var entity = new Area(request.FloorId, request.Name, request.Description);
        db.Areas.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, expand, cancellationToken))!;
    }

    public async Task<AreaResponse?> UpdateAsync(
        Guid id,
        AreaWriteRequest request,
        string? expand,
        CancellationToken cancellationToken)
    {
        var entity = await db.Areas.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.FloorId, request.Name, request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, expand, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Areas.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return false;
        db.Areas.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<AreaResponse>> ProjectAsync(
        IReadOnlyList<Area> entities,
        string? expand,
        CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var floorIds = entities.Select(item => item.FloorId).Distinct().ToArray();
        var floors = expansions.Contains("floor")
            ? await db.Floors.AsNoTracking()
                .Where(item => floorIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken)
            : [];

        return entities.Select(item => new AreaResponse(
            item.Id,
            item.FloorId,
            item.Name,
            item.Description,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            floors.TryGetValue(item.FloorId, out var floor)
                ? new AreaFloorResponse(floor.Id, floor.Name, floor.FloorNumber)
                : null)).ToList();
    }
}
