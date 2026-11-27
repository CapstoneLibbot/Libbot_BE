using LiBot.Application.Models;
using LiBot.Domain.Job;

namespace LiBot.Application.Interfaces;

public interface IWorkflowDataRepository
{
    Task<SimulationEndpointRecord?> GetSimulationEndpointAsync(CancellationToken cancellationToken);
    Task<SimulationMapRecord?> GetMapAsync(Guid mapId, CancellationToken cancellationToken);
    Task<WorkflowJobRecord?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<PatrolMapRecord?> GetPatrolMapAsync(Guid mapId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PatrolEndpointRecord>> GetPatrolEndpointsAsync(IReadOnlyCollection<Guid> endpointIds, CancellationToken cancellationToken);
    Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, int? levelNumber, CancellationToken cancellationToken);
    Task<InventoryBookRecord?> GetInventoryBookAsync(string barcode, CancellationToken cancellationToken);
    Task RecordInventoryAsync(string barcode, Guid? shelfLevelId, CancellationToken cancellationToken);
    Task<Guid> AddJobEventAsync(Guid jobId, string eventType, string? data, CancellationToken cancellationToken);
    Task<IReadOnlyList<string?>> GetJobEventDataAsync(Guid jobId, string eventType, CancellationToken cancellationToken);
    Task<int> CountMissingBooksAsync(IReadOnlyCollection<Guid> shelfLevelIds, IReadOnlyCollection<string> scannedBarcodes,
        CancellationToken cancellationToken);
    Task<Guid> SaveInventoryReportAsync(Guid jobId, string scopeType, int totalBooksScanned,
        int correctlyPlaced, int misplaced, int missing, string data, CancellationToken cancellationToken);
    Task<Guid> SavePatrolReportAsync(Guid jobId, int durationSeconds, int observationCount,
        int warnings, string data, CancellationToken cancellationToken);
}
