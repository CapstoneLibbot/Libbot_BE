namespace LiBot.Infrastructure.BackgroundJobs;

public class MissionDispatcherService
{
    public Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
