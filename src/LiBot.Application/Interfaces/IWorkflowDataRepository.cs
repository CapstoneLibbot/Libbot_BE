using LiBot.Application.Models;
using LiBot.Domain.Mission;

namespace LiBot.Application.Interfaces;

public interface IWorkflowDataRepository
{
    Task<SimulationEndpointRecord?> GetSimulationEndpointAsync(CancellationToken cancellationToken);
    Task<SimulationMapRecord?> GetMapAsync(Guid mapId, CancellationToken cancellationToken);
    Task<WorkflowMissionRecord?> GetMissionAsync(Guid missionId, CancellationToken cancellationToken);
    Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, CancellationToken cancellationToken);
    Task<InventoryBookRecord?> GetInventoryBookAsync(string barcode, CancellationToken cancellationToken);
    Task RecordInventoryAsync(string barcode, Guid? shelfLevelId, CancellationToken cancellationToken);
    Task AddJobEventAsync(Guid missionId, string eventType, string? data, CancellationToken cancellationToken);
    Task<IReadOnlyList<string?>> GetJobEventDataAsync(Guid missionId, string eventType, CancellationToken cancellationToken);
    Task<int> CountMissingBooksAsync(IReadOnlyCollection<Guid> shelfLevelIds, IReadOnlyCollection<string> scannedBarcodes,
        CancellationToken cancellationToken);
    Task<Guid> SaveInventoryReportAsync(Guid missionId, string reportType, string description, string scopeType,
        int totalBooksScanned, int correctlyPlaced, int misplaced, int missing, string eventData, CancellationToken cancellationToken);
    Task<Guid> SavePatrolReportAsync(Guid missionId, string reportType, string description, int durationSeconds,
        int observationCount, int totalFindings, int fallenBooks, int obstacles, int warnings, string status,
        string eventData, CancellationToken cancellationToken);
}
