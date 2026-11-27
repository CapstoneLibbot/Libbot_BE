using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Job;

namespace LiBot.Application.Services;

public sealed class WorkflowDataService(IWorkflowDataRepository repository) : IWorkflowDataService
{
    public Task<SimulationEndpointRecord?> GetSimulationEndpointAsync(CancellationToken cancellationToken) =>
        repository.GetSimulationEndpointAsync(cancellationToken);

    public Task<SimulationMapRecord?> GetMapAsync(Guid mapId, CancellationToken cancellationToken) =>
        repository.GetMapAsync(mapId, cancellationToken);

    public Task<WorkflowJobRecord?> GetJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        repository.GetJobAsync(jobId, cancellationToken);

    public Task<PatrolMapRecord?> GetPatrolMapAsync(Guid mapId, CancellationToken cancellationToken) =>
        repository.GetPatrolMapAsync(mapId, cancellationToken);

    public Task<IReadOnlyList<PatrolEndpointRecord>> GetPatrolEndpointsAsync(
        IReadOnlyCollection<Guid> endpointIds, CancellationToken cancellationToken) =>
        repository.GetPatrolEndpointsAsync(endpointIds, cancellationToken);

    public Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, int? levelNumber, CancellationToken cancellationToken) =>
        repository.ResolveShelfLevelAsync(shelfCode, levelNumber, cancellationToken);

    public Task<InventoryBookRecord?> GetInventoryBookAsync(string barcode, CancellationToken cancellationToken) =>
        repository.GetInventoryBookAsync(barcode, cancellationToken);

    public Task RecordInventoryAsync(string barcode, Guid? shelfLevelId, CancellationToken cancellationToken) =>
        repository.RecordInventoryAsync(barcode, shelfLevelId, cancellationToken);

    public Task<Guid> AddJobEventAsync(Guid jobId, string eventType, string? data, CancellationToken cancellationToken) =>
        repository.AddJobEventAsync(jobId, eventType, data, cancellationToken);

    public Task<IReadOnlyList<string?>> GetJobEventDataAsync(Guid jobId, string eventType, CancellationToken cancellationToken) =>
        repository.GetJobEventDataAsync(jobId, eventType, cancellationToken);

    public Task<int> CountMissingBooksAsync(IReadOnlyCollection<Guid> shelfLevelIds,
        IReadOnlyCollection<string> scannedBarcodes, CancellationToken cancellationToken) =>
        repository.CountMissingBooksAsync(shelfLevelIds, scannedBarcodes, cancellationToken);

    public Task<Guid> SaveInventoryReportAsync(Guid jobId, string scopeType, int totalBooksScanned,
        int correctlyPlaced, int misplaced, int missing, string data,
        CancellationToken cancellationToken) =>
        repository.SaveInventoryReportAsync(jobId, scopeType, totalBooksScanned,
            correctlyPlaced, misplaced, missing, data, cancellationToken);

    public Task<Guid> SavePatrolReportAsync(Guid jobId, int durationSeconds, int observationCount,
        int warnings, string data, CancellationToken cancellationToken) =>
        repository.SavePatrolReportAsync(jobId, durationSeconds, observationCount, warnings, data, cancellationToken);
}
