using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IPatrolNotificationPublisher
{
    Task PublishPatrolCompletedAsync(PatrolCompletedNotification notification, CancellationToken cancellationToken);
    Task PublishLeaningBookDetectedAsync(LeaningBookDetectedNotification notification, CancellationToken cancellationToken);
}
