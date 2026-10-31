using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class WorkflowDataRepository(LiBotDbContext db) : IWorkflowDataRepository
{
    public async Task<SimulationEndpointRecord?> GetSimulationEndpointAsync(CancellationToken cancellationToken)
    {
        var endpoint = await db.Endpoints.AsNoTracking().FirstOrDefaultAsync(item =>
            item.IsActive && EF.Functions.Like(item.Name, "%A3%"), cancellationToken);
        return endpoint is null ? null : new SimulationEndpointRecord(endpoint.Id, endpoint.Name, endpoint.MapId,
            endpoint.PositionX, endpoint.PositionY, endpoint.Theta);
    }

    public async Task<SimulationMapRecord?> GetMapAsync(Guid mapId, CancellationToken cancellationToken)
    {
        var map = await db.Maps.AsNoTracking().FirstOrDefaultAsync(item => item.Id == mapId, cancellationToken);
        return map is null ? null : new SimulationMapRecord(map.Id, map.Name, map.Width, map.Height);
    }

    public async Task<WorkflowMissionRecord?> GetMissionAsync(Guid missionId, CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.AsNoTracking().FirstOrDefaultAsync(item => item.JobId == missionId, cancellationToken);
        return task is null ? null : new WorkflowMissionRecord(task.RuntimeId, task.StartedAtUtc);
    }

    public async Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shelfCode)) return null;
        return await (from level in db.ShelfLevels.AsNoTracking()
                      join shelf in db.Shelves.AsNoTracking() on level.ShelfId equals shelf.Id
                      where shelf.Code == shelfCode
                      orderby level.LevelNumber
                      select (Guid?)level.Id).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<InventoryBookRecord?> GetInventoryBookAsync(string barcode, CancellationToken cancellationToken)
    {
        var item = await db.BookItems.AsNoTracking()
            .FirstOrDefaultAsync(bookItem => bookItem.Barcode == barcode, cancellationToken);
        if (item is null) return null;
        var expectedShelfCode = item.ExpectedShelfLevelId is Guid levelId
            ? await (from level in db.ShelfLevels.AsNoTracking()
                     join shelf in db.Shelves.AsNoTracking() on level.ShelfId equals shelf.Id
                     where level.Id == levelId
                     select shelf.Code).FirstOrDefaultAsync(cancellationToken)
            : null;
        return new InventoryBookRecord(item.ExpectedShelfLevelId, expectedShelfCode);
    }

    public async Task RecordInventoryAsync(string barcode, Guid? shelfLevelId, CancellationToken cancellationToken)
    {
        var item = await db.BookItems.FirstOrDefaultAsync(bookItem => bookItem.Barcode == barcode, cancellationToken);
        if (item is null) return;
        item.RecordInventory(shelfLevelId);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddJobEventAsync(Guid missionId, string eventType, string? data, CancellationToken cancellationToken)
    {
        await db.JobEvents.AddAsync(new JobEvent(missionId, eventType, data), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string?>> GetJobEventDataAsync(
        Guid missionId, string eventType, CancellationToken cancellationToken) =>
        await db.JobEvents.AsNoTracking()
            .Where(item => item.JobId == missionId && item.EventType == eventType)
            .OrderBy(item => item.EventTimeUtc)
            .Select(item => item.Data)
            .ToListAsync(cancellationToken);

    public Task<int> CountMissingBooksAsync(IReadOnlyCollection<Guid> shelfLevelIds,
        IReadOnlyCollection<string> scannedBarcodes, CancellationToken cancellationToken)
    {
        var levels = shelfLevelIds.ToArray();
        var barcodes = scannedBarcodes.ToArray();
        return db.BookItems.CountAsync(item => item.ExpectedShelfLevelId.HasValue
            && levels.Contains(item.ExpectedShelfLevelId.Value)
            && !barcodes.Contains(item.Barcode), cancellationToken);
    }

    public async Task<Guid> SaveInventoryReportAsync(Guid missionId, string scopeType,
        int totalBooksScanned, int correctlyPlaced, int misplaced, int missing, string data,
        CancellationToken cancellationToken)
    {
        var report = new InventoryReport(missionId, scopeType, 1,
            totalBooksScanned, correctlyPlaced, misplaced, missing, data);
        await db.InventoryReports.AddAsync(report, cancellationToken);
        await db.JobEvents.AddAsync(new JobEvent(missionId, "INVENTORY_REPORT", data), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return report.Id;
    }

    public async Task<Guid> SavePatrolReportAsync(Guid missionId, int durationSeconds, int observationCount,
        int warnings, string data, CancellationToken cancellationToken)
    {
        var report = new PatrolReport(missionId, durationSeconds, observationCount, warnings, data);
        await db.PatrolReports.AddAsync(report, cancellationToken);
        await db.JobEvents.AddAsync(new JobEvent(missionId, "PATROL_REPORT", data), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return report.Id;
    }
}
