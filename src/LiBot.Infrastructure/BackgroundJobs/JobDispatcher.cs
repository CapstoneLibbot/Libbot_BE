namespace LiBot.Infrastructure.BackgroundJobs;

public class JobDispatcher
{
    public Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
