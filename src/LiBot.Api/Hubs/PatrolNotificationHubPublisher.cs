using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using Microsoft.AspNetCore.SignalR;

namespace LiBot.Api.Hubs;

public sealed class PatrolNotificationHubPublisher(IHubContext<RobotHub> hub) : IPatrolNotificationPublisher
{
    public Task PublishPatrolCompletedAsync(PatrolCompletedNotification notification, CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.LibrarianGroup)
            .SendAsync("PATROL_COMPLETED", notification, cancellationToken);

    public Task PublishLeaningBookDetectedAsync(LeaningBookDetectedNotification notification, CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.LibrarianGroup).SendAsync("LibrarianNotification", new
        {
            type = "LEANING_BOOK_DETECTED",
            runtimeId = "real",
            missionId = notification.MissionId,
            executionId = notification.ExecutionId,
            evidenceId = notification.EvidenceId,
            imageUrl = notification.ImageUrl,
            pathId = notification.PathId,
            confidence = notification.Confidence,
            waypoint = notification.Waypoint,
            observedAtUtc = notification.ObservedAtUtc,
            message = $"Patrol detected a leaning book (confidence {notification.Confidence:P0}).",
            createdAt = notification.ObservedAtUtc
        }, cancellationToken);
}
