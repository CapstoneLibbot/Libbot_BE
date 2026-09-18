namespace LiBot.Infrastructure.BackgroundJobs;

public class RobotHeartbeatMonitor
{
    public Task CheckHeartbeatsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
