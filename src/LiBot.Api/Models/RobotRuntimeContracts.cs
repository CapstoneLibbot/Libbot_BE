using System.ComponentModel.DataAnnotations;

namespace LiBot.Api.Models;

public sealed record RobotPoseDto(double X, double Y, double Yaw);

public sealed record RobotTwistDto(double LinearX, double LinearY, double AngularZ);

public sealed record PlannedPathPointDto(double X, double Y);

public sealed record OccupancyGridDto(
    DateTimeOffset Timestamp,
    uint Width,
    uint Height,
    double Resolution,
    double OriginX,
    double OriginY,
    double OriginYaw,
    int[] Data);

public sealed record ManualVelocityRequest(
    [Range(-0.7, 0.7)] double LinearX,
    [Range(-0.7, 0.7)] double LinearY,
    [Range(-0.5, 0.5)] double AngularZ,
    string? ArmLeaseId);

public sealed record SaveRobotMapRequest(string Name);

public sealed record RobotTelemetryRequest(
    DateTimeOffset Timestamp,
    RobotPoseDto Pose,
    RobotTwistDto? Twist,
    double? Battery,
    string LocalizationState,
    string NavigationState,
    string? ActiveGoalId,
    IReadOnlyList<PlannedPathPointDto>? PlannedPath,
    string? Error,
    bool MappingActive = false,
    bool NavigationActive = false);

public sealed record RobotRuntimeResponse(
    string Id,
    string DisplayName,
    string Kind,
    bool Connected,
    bool Stale,
    bool Armed,
    bool MappingActive,
    bool NavigationActive,
    DateTimeOffset? ArmExpiresAt,
    DateTimeOffset? LastTelemetryAt,
    RobotTelemetryRequest? Telemetry);

public sealed record ArmRuntimeResponse(string RuntimeId, string LeaseId, DateTimeOffset ExpiresAt);

public sealed record InitialPoseRequest(
    [Range(-1000, 1000)] double X,
    [Range(-1000, 1000)] double Y,
    [Range(-100, 100)] double Yaw,
    string? ArmLeaseId = null);

public sealed record NavigationGoalRequest(
    [Range(-1000, 1000)] double X,
    [Range(-1000, 1000)] double Y,
    [Range(-100, 100)] double Yaw,
    string? ArmLeaseId,
    string SpeedMode = "NORMAL",
    string ObstaclePolicy = "REROUTE",
    [Range(1, 600)] int TimeoutSec = 180);

public sealed record NavigationGoalResponse(string RuntimeId, string CommandId, string Status);
