using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using Microsoft.AspNetCore.SignalR;

namespace LiBot.Api.Hubs;

public sealed class PatrolNotificationHubPublisher(IHubContext<RobotHub> hub) : IPatrolNotificationPublisher
{
    public Task PublishPatrolCompletedAsync(PatrolCompletedNotification notification, CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.LibrarianGroup)
            .SendAsync("PATROL_COMPLETED", notification, cancellationToken);
}
