using LiBot.Application.Interfaces;
using LiBot.Application.Models.Floor;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class FloorRepository(LiBotDbContext db) : IFloorRepository
{
    public async Task<(IReadOnlyList<FloorResponse> Items, int TotalCount)> SearchAsync(FloorQuery query, CancellationToken cancellationToken)
    {
        var source = db.Floors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(item => EF.Functions.Like(item.Name, $"%{search}%"));
        }
        var total = await source.CountAsync(cancellationToken);
        var descending = query.SortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "name" => descending ? source.OrderByDescending(item => item.Name) : source.OrderBy(item => item.Name),
            "created" or "createdatutc" => descending ? source.OrderByDescending(item => item.CreatedAtUtc) : source.OrderBy(item => item.CreatedAtUtc),
            _ => descending ? source.OrderByDescending(item => item.FloorNumber) : source.OrderBy(item => item.FloorNumber)
        };
        var entities = await source.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return (await ProjectAsync(entities, query.Expand, cancellationToken), total);
    }

    public async Task<FloorResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Floors.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : (await ProjectAsync([entity], expand, cancellationToken)).Single();
    }

    public async Task<FloorResponse> CreateAsync(FloorWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        var entity = new Floor(request.Name, request.FloorNumber, request.Description, request.Status ?? LiBot.Domain.Enums.FloorStatus.Active);
        db.Floors.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, expand, cancellationToken))!;
    }

    public async Task<FloorResponse?> UpdateAsync(Guid id, FloorWriteRequest request, string? expand, CancellationToken cancellationToken)
    {
        var entity = await db.Floors.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return null;
        entity.Update(request.Name, request.FloorNumber, request.Description, request.Status);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, expand, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Floors.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null) return false;
        db.Floors.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<FloorResponse>> ProjectAsync(IReadOnlyList<Floor> entities, string? expand, CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ids = entities.Select(item => item.Id).ToArray();
        var aisles = expansions.Contains("aisles")
            ? await db.Aisles.AsNoTracking().Where(item => ids.Contains(item.FloorId)).ToListAsync(cancellationToken)
            : [];
        var areas = expansions.Contains("areas")
            ? await db.Areas.AsNoTracking().Where(item => ids.Contains(item.FloorId)).ToListAsync(cancellationToken)
            : [];
        return entities.Select(item => new FloorResponse(item.Id, item.Name, item.FloorNumber, item.Description, item.Status, item.CreatedAtUtc, item.UpdatedAtUtc,
            expansions.Contains("aisles") ? aisles.Where(value => value.FloorId == item.Id).Select(value => new FloorAisleResponse(value.Id, value.Name)).ToList() : null,
            expansions.Contains("areas") ? areas.Where(value => value.FloorId == item.Id).Select(value => new FloorAreaResponse(value.Id, value.Name, value.Description)).ToList() : null)).ToList();
    }
}
