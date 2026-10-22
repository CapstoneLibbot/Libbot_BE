namespace LiBot.Application.Interfaces;

public interface IRobotTaskRepository
{
    Task<bool> IsSimulationTaskAsync(Guid taskId, CancellationToken cancellationToken);
}
