using LiBot.Application.Interfaces;
using LiBot.Application.Models.Zone;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class ZoneRepository(LiBotDbContext db) : IZoneRepository
{
    public async Task<(IReadOnlyList<ZoneResponse> Items, int TotalCount)> SearchAsync(ZoneQuery query, CancellationToken cancellationToken)
    {
        var source = db.Zones.AsNoTracking().AsQueryable();
        if (query.MapId is Guid mapId) source = source.Where(item => item.MapId == mapId);
        if (query.IsActive is bool active) source = source.Where(item => item.IsActive == active);
        if (!string.IsNullOrWhiteSpace(query.ZoneType)) source = source.Where(item => item.ZoneType == query.ZoneType.Trim().ToUpper());
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(item => EF.Functions.Like(item.ZoneType, $"%{search}%")
                || EF.Functions.Like(item.Name, $"%{search}%")
                || (item.Description != null && EF.Functions.Like(item.Description, $"%{search}%")));
        }
        var total = await source.CountAsync(cancellationToken);
        var descending = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "maxspeed" => descending ? source.OrderByDescending(item => item.MaxSpeed) : source.OrderBy(item => item.MaxSpeed),
            "created" or "createdatutc" => descending ? source.OrderByDescending(item => item.CreatedAtUtc) : source.OrderBy(item => item.CreatedAtUtc),
            _ => descending ? source.OrderByDescending(item => item.ZoneType) : source.OrderBy(item => item.ZoneType)
        };
        var entities = await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (await ProjectAsync(entities, query.Expand, cancellationToken), total);
    }

    public async Task<ZoneResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Zones.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : (await ProjectAsync([entity], expand, cancellationToken)).Single();
    }

    public Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken) => db.Maps.AnyAsync(item => item.Id == id, cancellationToken);

    public async Task<ZoneResponse> CreateAsync(ZoneWriteRequest request, CancellationToken cancellationToken)
    {
        var entity = new Zone(request.MapId, request.ZoneType, request.MinX, request.MinY, request.MaxX, request.MaxY,
            request.MaxSpeed, name: request.Name, description: request.Description);
        if (!request.IsActive) entity.Deactivate();
        db.Zones.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, null, cancellationToken))!;
    }

    public async Task<ZoneResponse?> UpdateAsync(Guid id, ZoneWriteRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Zones.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.MapId, request.ZoneType, request.MinX, request.MinY, request.MaxX, request.MaxY,
            request.MaxSpeed, null, null, false, request.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, null, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Zones.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return false;
        db.Zones.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<ZoneResponse>> ProjectAsync(IReadOnlyList<Zone> entities, string? expand, CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ids = entities.Select(item => item.MapId).Distinct().ToArray();
        var maps = expansions.Contains("map")
            ? await db.Maps.AsNoTracking().Where(item => ids.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken)
            : [];
        return entities.Select(item => new ZoneResponse(item.Id, item.MapId, item.Name ?? item.ZoneType, item.Description, item.ZoneType, item.MaxSpeed,
            item.MinX, item.MinY, item.MaxX, item.MaxY, item.IsActive, item.CreatedAtUtc,
            maps.TryGetValue(item.MapId, out var map) ? new ZoneMapResponse(map.Id, map.Name, map.Version, map.Width, map.Height) : null)).ToList();
    }
}
