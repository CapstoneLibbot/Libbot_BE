using System.ComponentModel.DataAnnotations;

namespace LiBot.Api.Models;

public sealed record RobotPoseDto(double X, double Y, double Yaw);

public sealed record RobotTwistDto(double LinearX, double LinearY, double AngularZ);

public sealed record PlannedPathPointDto(double X, double Y);

public sealed record RobotTelemetryRequest(
    DateTimeOffset Timestamp,
    RobotPoseDto Pose,
    RobotTwistDto? Twist,
    double? Battery,
    string LocalizationState,
    string NavigationState,
    string? ActiveGoalId,
    IReadOnlyList<PlannedPathPointDto>? PlannedPath,
    string? Error);

public sealed record RobotRuntimeResponse(
    string Id,
    string DisplayName,
    string Kind,
    bool Connected,
    bool Stale,
    bool Armed,
    DateTimeOffset? ArmExpiresAt,
    DateTimeOffset? LastTelemetryAt,
    RobotTelemetryRequest? Telemetry);

public sealed record ArmRuntimeResponse(string RuntimeId, string LeaseId, DateTimeOffset ExpiresAt);

public sealed record InitialPoseRequest(
    [property: Range(-1000, 1000)] double X,
    [property: Range(-1000, 1000)] double Y,
    [property: Range(-100, 100)] double Yaw,
    string? ArmLeaseId = null);

public sealed record NavigationGoalRequest(
    [property: Range(-1000, 1000)] double X,
    [property: Range(-1000, 1000)] double Y,
    [property: Range(-100, 100)] double Yaw,
    string? ArmLeaseId,
    string SpeedMode = "NORMAL",
    string ObstaclePolicy = "REROUTE",
    [property: Range(1, 600)] int TimeoutSec = 180);

public sealed record NavigationGoalResponse(string RuntimeId, string CommandId, string Status);

