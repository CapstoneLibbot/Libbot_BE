using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using Microsoft.AspNetCore.SignalR;

namespace LiBot.Api.Hubs;

public sealed class RobotRuntimeHubPublisher(IHubContext<RobotHub> hub) : IRobotRuntimeEventPublisher
{
    public async Task PublishTelemetryUpdatedAsync(
        string runtimeId,
        RobotRuntimeResponse snapshot,
        RobotTelemetryRequest telemetry,
        CancellationToken cancellationToken)
    {
        var clients = hub.Clients.Group(RobotHub.RuntimeGroup(runtimeId));
        await clients.SendAsync("RobotTelemetryUpdated", snapshot, cancellationToken);
        await clients.SendAsync(
            "RobotPositionUpdated",
            new { robotId = runtimeId, telemetry.Pose.X, telemetry.Pose.Y, telemetry.Pose.Yaw },
            cancellationToken);
        await clients.SendAsync(
            "NavigationStatusChanged",
            new { runtimeId, telemetry.NavigationState, telemetry.ActiveGoalId, telemetry.Error },
            cancellationToken);
        if (telemetry.PlannedPath is { Count: > 0 })
        {
            await clients.SendAsync(
                "PlannedPathUpdated",
                new { runtimeId, points = telemetry.PlannedPath },
                cancellationToken);
        }
    }

    public Task PublishMapUpdatedAsync(string runtimeId, OccupancyGridDto map, CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.RuntimeGroup(runtimeId))
            .SendAsync("RobotMapUpdated", map, cancellationToken);

    public Task PublishConnectionChangedAsync(
        string runtimeId,
        RobotRuntimeResponse snapshot,
        CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.RuntimeGroup(runtimeId))
            .SendAsync("RuntimeConnectionChanged", snapshot, cancellationToken);
}
