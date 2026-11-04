using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;

namespace LiBot.Application.Services;

public sealed class WorkflowDataService(IWorkflowDataRepository repository) : IWorkflowDataService
{
    public Task<SimulationEndpointRecord?> GetSimulationEndpointAsync(CancellationToken cancellationToken) =>
        repository.GetSimulationEndpointAsync(cancellationToken);

    public Task<SimulationMapRecord?> GetMapAsync(Guid mapId, CancellationToken cancellationToken) =>
        repository.GetMapAsync(mapId, cancellationToken);

    public Task<WorkflowMissionRecord?> GetMissionAsync(Guid missionId, CancellationToken cancellationToken) =>
        repository.GetMissionAsync(missionId, cancellationToken);

    public Task<PatrolMapRecord?> GetPatrolMapAsync(Guid mapId, CancellationToken cancellationToken) =>
        repository.GetPatrolMapAsync(mapId, cancellationToken);

    public Task<IReadOnlyList<PatrolEndpointRecord>> GetPatrolEndpointsAsync(
        IReadOnlyCollection<Guid> endpointIds, CancellationToken cancellationToken) =>
        repository.GetPatrolEndpointsAsync(endpointIds, cancellationToken);

    public Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, CancellationToken cancellationToken) =>
        repository.ResolveShelfLevelAsync(shelfCode, cancellationToken);

    public Task<InventoryBookRecord?> GetInventoryBookAsync(string barcode, CancellationToken cancellationToken) =>
        repository.GetInventoryBookAsync(barcode, cancellationToken);

    public Task RecordInventoryAsync(string barcode, Guid? shelfLevelId, CancellationToken cancellationToken) =>
        repository.RecordInventoryAsync(barcode, shelfLevelId, cancellationToken);

    public Task<Guid> AddJobEventAsync(Guid missionId, string eventType, string? data, CancellationToken cancellationToken) =>
        repository.AddJobEventAsync(missionId, eventType, data, cancellationToken);

    public Task<IReadOnlyList<string?>> GetJobEventDataAsync(Guid missionId, string eventType, CancellationToken cancellationToken) =>
        repository.GetJobEventDataAsync(missionId, eventType, cancellationToken);

    public Task<int> CountMissingBooksAsync(IReadOnlyCollection<Guid> shelfLevelIds,
        IReadOnlyCollection<string> scannedBarcodes, CancellationToken cancellationToken) =>
        repository.CountMissingBooksAsync(shelfLevelIds, scannedBarcodes, cancellationToken);

    public Task<Guid> SaveInventoryReportAsync(Guid missionId, string scopeType, int totalBooksScanned,
        int correctlyPlaced, int misplaced, int missing, string data,
        CancellationToken cancellationToken) =>
        repository.SaveInventoryReportAsync(missionId, scopeType, totalBooksScanned,
            correctlyPlaced, misplaced, missing, data, cancellationToken);

    public Task<Guid> SavePatrolReportAsync(Guid missionId, int durationSeconds, int observationCount,
        int warnings, string data, CancellationToken cancellationToken) =>
        repository.SavePatrolReportAsync(missionId, durationSeconds, observationCount, warnings, data, cancellationToken);
}
