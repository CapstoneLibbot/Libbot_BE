namespace LiBot.Application.Models;

public sealed record LeaningBookDetectedNotification(
    Guid MissionId,
    string ExecutionId,
    string PathId,
    Guid EvidenceId,
    string ImageUrl,
    double Confidence,
    object? Waypoint,
    DateTime ObservedAtUtc);
