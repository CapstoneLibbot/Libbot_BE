using LiBot.Application.Models;

namespace LiBot.Application.Services.Robot;

public interface IRobotRuntimeService
{
    IReadOnlyList<RobotRuntimeResponse> GetAll();
    RobotRuntimeResponse? Get(string runtimeId);
    string? ValidateBookGuidanceStart(string runtimeId, string? armLeaseId);
    ArmRuntimeResponse? Arm(string runtimeId);
    bool Disarm(string runtimeId);
    Task<bool> UpdateTelemetryAsync(string runtimeId, RobotTelemetryRequest telemetry, CancellationToken cancellationToken);
    Task<bool> UpdateMapAsync(string runtimeId, OccupancyGridDto map, CancellationToken cancellationToken);
    OccupancyGridDto? GetLatestMap(string runtimeId);
    Task<CommandResult> SendManualVelocityAsync(string runtimeId, ManualVelocityRequest request, CancellationToken cancellationToken);
    Task<CommandResult> StopManualAsync(string runtimeId, bool resumeNavigation, CancellationToken cancellationToken);
    Task<CommandResult> SetMappingAsync(string runtimeId, bool enabled, string? leaseId, CancellationToken cancellationToken);
    Task<CommandResult> SaveMapOnRobotAsync(string runtimeId, string fileStem, string? leaseId, CancellationToken cancellationToken);
    Task<CommandResult> StartNavigationModeAsync(string runtimeId, Guid mapId, string mapPath, string? leaseId, CancellationToken cancellationToken);
    Task<CommandResult> StopNavigationModeAsync(string runtimeId, CancellationToken cancellationToken);
    Task<CommandResult> SetInitialPoseAsync(string runtimeId, InitialPoseRequest request, CancellationToken cancellationToken);
    Task<CommandResult> StartNavigationAsync(string runtimeId, NavigationGoalRequest request, CancellationToken cancellationToken);
    Task<CommandResult> CancelNavigationAsync(string runtimeId, string commandId, string? armLeaseId, CancellationToken cancellationToken);
}
