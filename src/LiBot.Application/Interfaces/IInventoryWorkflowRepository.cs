using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IInventoryWorkflowRepository
{
    Task<IReadOnlyList<InventoryShelfScopeRecord>> ResolveScopeAsync(
        Guid floorId,
        Guid? mapId,
        IReadOnlyCollection<Guid> targetShelfIds,
        CancellationToken cancellationToken);

    Task<InventoryShelfScopeRecord?> GetShelfScopeAsync(Guid shelfId, CancellationToken cancellationToken);

    Task<IReadOnlyList<InventoryBookItemRecord>> GetBookItemsAsync(
        IReadOnlyCollection<Guid> shelfLevelIds,
        IReadOnlyCollection<string> qrCodes,
        CancellationToken cancellationToken);

    Task<int> UpdateBookItemsAsync(
        IReadOnlyCollection<InventoryBookItemUpdate> updates,
        CancellationToken cancellationToken);

    Task<Guid> AddJobEventAsync(Guid jobId, string eventType, string? data, CancellationToken cancellationToken);

    Task<IReadOnlyList<string?>> GetJobEventDataAsync(
        Guid jobId,
        string eventType,
        CancellationToken cancellationToken);

    Task<Guid> SaveInventoryReportAsync(
        Guid jobId,
        string scopeType,
        int totalShelves,
        int totalBooksScanned,
        int totalAvailable,
        int totalMisplaced,
        int totalMissing,
        string data,
        CancellationToken cancellationToken);

    Task<InventoryReportRecord?> GetInventoryReportAsync(Guid jobId, CancellationToken cancellationToken);
}
