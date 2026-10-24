namespace LiBot.Application.Models;

public sealed record PatrolCompletedNotification(
    Guid MissionId,
    Guid ReportId,
    string ReportType,
    string RuntimeId,
    int ObservationCount,
    int FallenBookCount,
    int UnknownObstacleCount,
    int OtherWarningCount,
    DateTime CompletedAtUtc);
