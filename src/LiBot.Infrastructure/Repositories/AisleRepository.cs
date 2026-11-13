using LiBot.Application.Interfaces;
using LiBot.Application.Models.Aisle;
using LiBot.Domain.Enums;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class AisleRepository(LiBotDbContext db) : IAisleRepository
{
    public async Task<(IReadOnlyList<AisleResponse> Items, int TotalCount)> SearchAsync(
        AisleQuery query,
        CancellationToken cancellationToken)
    {
        var source = db.Aisles
            .AsNoTracking()
            .AsQueryable();

        if (query.FloorId is Guid floorId)
        {
            source = source.Where(item => item.FloorId == floorId);
        }

        if (query.Status is not null)
        {
            if (Enum.TryParse<AisleStatus>(query.Status, true, out var status))
                source = source.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            source = source.Where(item =>
                EF.Functions.Like(item.Name, $"%{search}%")
                || (item.Description != null
                    && EF.Functions.Like(item.Description, $"%{search}%")));
        }

        var total = await source.CountAsync(cancellationToken);

        var descending = string.Equals(
            query.SortOrder,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        source = query.SortBy.ToLowerInvariant() switch
        {
            "createdatutc" or "created" =>
                descending
                    ? source.OrderByDescending(item => item.CreatedAtUtc)
                    : source.OrderBy(item => item.CreatedAtUtc),

            "status" =>
                descending
                    ? source.OrderByDescending(item => item.Status)
                    : source.OrderBy(item => item.Status),

            _ =>
                descending
                    ? source.OrderByDescending(item => item.Name)
                    : source.OrderBy(item => item.Name)
        };

        var entities = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (
            await ProjectAsync(entities, query.Expand, cancellationToken),
            total
        );
    }

    public async Task<AisleResponse?> GetByIdAsync(
        Guid id,
        string? expand,
        CancellationToken cancellationToken)
    {
        var entity = await db.Aisles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (entity is null)
            return null;

        return (await ProjectAsync(
            [entity],
            expand,
            cancellationToken)).Single();
    }

    public Task<bool> FloorExistsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return db.Floors.AnyAsync(
            item => item.Id == id,
            cancellationToken);
    }

    public async Task<AisleResponse> CreateAsync(
        AisleWriteRequest request,
        string? expand,
        CancellationToken cancellationToken)
    {
        var entity = new Aisle(
            request.FloorId,
            request.Name,
            request.Description);

        db.Aisles.Add(entity);

        await db.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(
            entity.Id,
            expand,
            cancellationToken))!;
    }

    public async Task<AisleResponse?> UpdateAsync(
        Guid id,
        AisleWriteRequest request,
        string? expand,
        CancellationToken cancellationToken)
    {
        var entity = await db.Aisles
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (entity is null)
            return null;

        entity.Update(
            request.FloorId,
            request.Name,
            request.Description,
            Enum.TryParse<AisleStatus>(request.Status, true, out var status) ? status : AisleStatus.Active);

        await db.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(
            id,
            expand,
            cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await db.Aisles
            .FirstOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        if (entity is null)
            return false;

        db.Aisles.Remove(entity);

        await db.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<IReadOnlyList<AisleResponse>> ProjectAsync(
        IReadOnlyList<Aisle> entities,
        string? expand,
        CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var floorIds = entities
            .Select(item => item.FloorId)
            .Distinct()
            .ToArray();

        var aisleIds = entities
            .Select(item => item.Id)
            .ToArray();

        var floors = expansions.Contains("floor")
            ? await db.Floors
                .AsNoTracking()
                .Where(item => floorIds.Contains(item.Id))
                .ToDictionaryAsync(
                    item => item.Id,
                    cancellationToken)
            : new Dictionary<Guid, Floor>();

        var shelves = expansions.Contains("shelves")
            ? await db.Shelves
                .AsNoTracking()
                .Where(item => aisleIds.Contains(item.AisleId))
                .ToListAsync(cancellationToken)
            : [];

        return entities.Select(item =>
        {
            var floorResponse =
                floors.TryGetValue(item.FloorId, out var floor)
                    ? new AisleFloorResponse(
                        floor.Id,
                        floor.Name,
                        floor.FloorNumber)
                    : null;

            var shelfResponses = expansions.Contains("shelves")
                ? shelves
                    .Where(shelf => shelf.AisleId == item.Id)
                    .Select(shelf => new AisleShelfResponse(
                        shelf.Id,
                        shelf.Name))
                    .ToList()
                : null;

            return new AisleResponse(
                item.Id,
                item.FloorId,
                item.Name,
                item.Description,
                item.Status.ToString(),
                item.CreatedAtUtc,
                item.UpdatedAtUtc,
                floorResponse,
                shelfResponses);
        }).ToList();
    }
}
