namespace LiBot.Infrastructure.BackgroundJobs;

public class JobTimeoutMonitor
{
    public Task CheckTimeoutsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
