namespace LiBot.Infrastructure.BackgroundJobs;

public class MissionTimeoutMonitor
{
    public Task CheckTimeoutsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
