namespace LiBot.Infrastructure.BackgroundJobs;

public class MissionDispatcher
{
    public Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
