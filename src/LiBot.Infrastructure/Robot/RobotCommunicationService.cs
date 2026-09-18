namespace LiBot.Infrastructure.Robot;

public class RobotCommunicationService
{
    public Task SendCommandAsync(string robotId, string command, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(robotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        return Task.CompletedTask;
    }

    public Task<string> GetStatusAsync(string robotId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(robotId);

        return Task.FromResult($"online:{robotId}");
    }
}
