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

    public Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, CancellationToken cancellationToken) =>
        repository.ResolveShelfLevelAsync(shelfCode, cancellationToken);

    public Task<InventoryBookRecord?> GetInventoryBookAsync(string barcode, CancellationToken cancellationToken) =>
        repository.GetInventoryBookAsync(barcode, cancellationToken);

    public Task RecordInventoryAsync(string barcode, Guid? shelfLevelId, CancellationToken cancellationToken) =>
        repository.RecordInventoryAsync(barcode, shelfLevelId, cancellationToken);

    public Task AddJobEventAsync(Guid missionId, string eventType, string? data, CancellationToken cancellationToken) =>
        repository.AddJobEventAsync(missionId, eventType, data, cancellationToken);

    public Task<IReadOnlyList<string?>> GetJobEventDataAsync(Guid missionId, string eventType, CancellationToken cancellationToken) =>
        repository.GetJobEventDataAsync(missionId, eventType, cancellationToken);

    public Task<int> CountMissingBooksAsync(IReadOnlyCollection<Guid> shelfLevelIds,
        IReadOnlyCollection<string> scannedBarcodes, CancellationToken cancellationToken) =>
        repository.CountMissingBooksAsync(shelfLevelIds, scannedBarcodes, cancellationToken);

    public Task<Guid> SaveInventoryReportAsync(Guid missionId, string reportType, string description, string scopeType,
        int totalBooksScanned, int correctlyPlaced, int misplaced, int missing, string eventData,
        CancellationToken cancellationToken) =>
        repository.SaveInventoryReportAsync(missionId, reportType, description, scopeType,
            totalBooksScanned, correctlyPlaced, misplaced, missing, eventData, cancellationToken);

    public Task<Guid> SavePatrolReportAsync(Guid missionId, string reportType, string description, int durationSeconds,
        int observationCount, int totalFindings, int fallenBooks, int obstacles, int warnings, string status,
        string eventData, CancellationToken cancellationToken) =>
        repository.SavePatrolReportAsync(missionId, reportType, description, durationSeconds, observationCount,
            totalFindings, fallenBooks, obstacles, warnings, status, eventData, cancellationToken);
}
