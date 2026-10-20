using LiBot.Application.Models;

namespace LiBot.Application.Services.Robot;

public interface IRobotRuntimeEventPublisher
{
    Task PublishTelemetryUpdatedAsync(string runtimeId, RobotRuntimeResponse snapshot, RobotTelemetryRequest telemetry, CancellationToken cancellationToken);
    Task PublishMapUpdatedAsync(string runtimeId, OccupancyGridDto map, CancellationToken cancellationToken);
    Task PublishConnectionChangedAsync(string runtimeId, RobotRuntimeResponse snapshot, CancellationToken cancellationToken);
}
