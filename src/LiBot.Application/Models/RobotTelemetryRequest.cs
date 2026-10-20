
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

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
