using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Library;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class InventoryWorkflowRepository(LiBotDbContext db) : IInventoryWorkflowRepository
{
    public async Task<IReadOnlyList<InventoryShelfScopeRecord>> ResolveScopeAsync(
        Guid floorId,
        Guid? mapId,
        IReadOnlyCollection<Guid> targetShelfIds,
        CancellationToken cancellationToken)
    {
        var requestedIds = targetShelfIds.Distinct().ToArray();
        if (floorId == Guid.Empty || requestedIds.Length == 0 || requestedIds.Any(id => id == Guid.Empty))
            throw new ArgumentException("A floor and at least one valid shelf are required.");

        var shelves = await (from shelf in db.Shelves.AsNoTracking()
                             join aisle in db.Aisles.AsNoTracking() on shelf.AisleId equals aisle.Id
                             join endpoint in db.Endpoints.AsNoTracking() on shelf.StartEndpointId equals endpoint.Id
                             join endEndpoint in db.Endpoints.AsNoTracking() on shelf.EndEndpointId equals endEndpoint.Id
                             join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
                             where requestedIds.Contains(shelf.Id)
                                 && aisle.FloorId == floorId
                                 && aisle.Status.ToLower() == "active"
                                 && shelf.Status.ToLower() == "active"
                                 && endpoint.IsActive
                                 && endEndpoint.IsActive
                                 && endEndpoint.MapId == endpoint.MapId
                                 && (!mapId.HasValue || endpoint.MapId == mapId.Value)
                             orderby aisle.Name, shelf.Name
                             select new { Shelf = shelf, Aisle = aisle, Endpoint = endpoint, EndEndpoint = endEndpoint, Map = map })
            .ToListAsync(cancellationToken);

        if (shelves.Count != requestedIds.Length)
            throw new InvalidOperationException("Every selected shelf must belong to the chosen floor and have an active navigation endpoint on the selected map.");

        var shelfIds = shelves.Select(item => item.Shelf.Id).ToArray();
        var levels = await db.ShelfLevels.AsNoTracking()
            .Where(item => shelfIds.Contains(item.ShelfId) && item.Status.ToLower() == "active")
            .OrderBy(item => item.LevelNumber)
            .Select(item => new { item.Id, item.ShelfId, item.LevelNumber })
            .ToListAsync(cancellationToken);

        return shelves.Select(item => CreateScopeRecord(
            item.Shelf.Id,
            item.Shelf.Name,
            item.Shelf.Name,
            item.Endpoint,
            item.EndEndpoint,
            levels.Where(level => level.ShelfId == item.Shelf.Id)
                .Select(level => new InventoryShelfLevelRecord(level.Id, level.LevelNumber))
                .ToArray())).ToArray();
    }

    public async Task<InventoryShelfScopeRecord?> GetShelfScopeAsync(Guid shelfId, CancellationToken cancellationToken)
    {
        var item = await (from shelf in db.Shelves.AsNoTracking()
                          join endpoint in db.Endpoints.AsNoTracking() on shelf.StartEndpointId equals endpoint.Id
                          join endEndpoint in db.Endpoints.AsNoTracking() on shelf.EndEndpointId equals endEndpoint.Id
                          where shelf.Id == shelfId
                              && shelf.Status.ToLower() == "active"
                              && endpoint.IsActive
                              && endEndpoint.IsActive
                              && endEndpoint.MapId == endpoint.MapId
                          select new
                          {
                              shelf.Id,
                              Code = shelf.Name,
                              shelf.Name,
                              Endpoint = endpoint,
                              EndEndpoint = endEndpoint
                          }).FirstOrDefaultAsync(cancellationToken);
        if (item is null) return null;

        var levels = await db.ShelfLevels.AsNoTracking()
            .Where(level => level.ShelfId == shelfId && level.Status.ToLower() == "active")
            .OrderBy(level => level.LevelNumber)
            .Select(level => new InventoryShelfLevelRecord(level.Id, level.LevelNumber))
            .ToListAsync(cancellationToken);
        return CreateScopeRecord(item.Id, item.Code, item.Name, item.Endpoint, item.EndEndpoint, levels);
    }

    public async Task<IReadOnlyList<InventoryBookItemRecord>> GetBookItemsAsync(
        IReadOnlyCollection<Guid> shelfLevelIds,
        IReadOnlyCollection<string> qrCodes,
        CancellationToken cancellationToken)
    {
        var levels = shelfLevelIds.Distinct().ToArray();
        var codes = qrCodes.Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).Distinct().ToArray();
        if (levels.Length == 0 && codes.Length == 0) return Array.Empty<InventoryBookItemRecord>();

        return await db.BookItems.AsNoTracking()
            .Where(item => (item.ExpectedShelfLevelId.HasValue && levels.Contains(item.ExpectedShelfLevelId.Value))
                || codes.Contains(item.Barcode))
            .Select(item => new InventoryBookItemRecord(
                item.Id,
                item.Barcode,
                item.ExpectedShelfLevelId,
                item.CurrentShelfLevelId,
                item.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> UpdateBookItemsAsync(
        IReadOnlyCollection<InventoryBookItemUpdate> updates,
        CancellationToken cancellationToken)
    {
        var byId = updates.Where(item => item.BookItemId != Guid.Empty)
            .GroupBy(item => item.BookItemId)
            .Select(group => group.Last())
            .ToDictionary(item => item.BookItemId);
        if (byId.Count == 0) return 0;

        var ids = byId.Keys.ToArray();
        var items = await db.BookItems.Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            var update = byId[item.Id];
            item.ApplyInventoryResult(update.ShelfLevelId, update.Status);
        }

        await db.SaveChangesAsync(cancellationToken);
        return items.Count;
    }

    public async Task<Guid> AddJobEventAsync(
        Guid missionId,
        string eventType,
        string? data,
        CancellationToken cancellationToken)
    {
        var jobEvent = new JobEvent(missionId, eventType, data);
        await db.JobEvents.AddAsync(jobEvent, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return jobEvent.Id;
    }

    public async Task<IReadOnlyList<string?>> GetJobEventDataAsync(
        Guid missionId,
        string eventType,
        CancellationToken cancellationToken) =>
        await db.JobEvents.AsNoTracking()
            .Where(item => item.JobId == missionId && item.EventType == eventType)
            .OrderBy(item => item.EventTimeUtc)
            .Select(item => item.Data)
            .ToListAsync(cancellationToken);

    public async Task<Guid> SaveInventoryReportAsync(
        Guid missionId,
        string scopeType,
        int totalShelves,
        int totalBooksScanned,
        int totalAvailable,
        int totalMisplaced,
        int totalMissing,
        string data,
        CancellationToken cancellationToken)
    {
        var report = new InventoryReport(missionId, scopeType, totalShelves,
            totalBooksScanned, totalAvailable, totalMisplaced, totalMissing, data);
        await db.InventoryReports.AddAsync(report, cancellationToken);
        await db.JobEvents.AddAsync(new JobEvent(missionId, "INVENTORY_REPORT", data), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return report.Id;
    }

    public async Task<InventoryReportRecord?> GetInventoryReportAsync(Guid missionId, CancellationToken cancellationToken)
    {
        var report = await db.InventoryReports.AsNoTracking()
            .Where(item => item.JobId == missionId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return report is null
            ? null
            : new InventoryReportRecord(report.Id, missionId, report.ScopeType, report.TotalShelves,
                report.TotalBooksScanned, report.TotalAvailable, report.TotalMisplaced, report.TotalMissing,
                report.Data ?? "{}", report.CreatedAtUtc);
    }

    private static InventoryShelfScopeRecord CreateScopeRecord(
        Guid shelfId,
        string code,
        string name,
        Endpoint startEndpoint,
        Endpoint endEndpoint,
        IReadOnlyList<InventoryShelfLevelRecord> levels)
    {
        if (levels.Count != 2 || !levels.Any(item => item.LevelNumber == 1) || !levels.Any(item => item.LevelNumber == 2))
            throw new InvalidOperationException($"Shelf '{code}' must have exactly level 1 and level 2 configured for the two-tier scanner.");

        var start = new InventoryNavigationTargetRecord(startEndpoint.Id, startEndpoint.Name,
            startEndpoint.PositionX, startEndpoint.PositionY, startEndpoint.Theta, startEndpoint.MapId);
        var end = new InventoryNavigationTargetRecord(endEndpoint.Id, endEndpoint.Name,
            endEndpoint.PositionX, endEndpoint.PositionY, endEndpoint.Theta, endEndpoint.MapId);
        InventoryEndpointValidator.Validate(start, end);
        return new InventoryShelfScopeRecord(shelfId, code, name, start,
            levels.OrderBy(item => item.LevelNumber).ToArray(), start, end);
    }
}
