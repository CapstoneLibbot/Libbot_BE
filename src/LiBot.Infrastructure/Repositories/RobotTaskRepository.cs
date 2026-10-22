using LiBot.Application.Interfaces;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class RobotTaskRepository(LiBotDbContext db) : IRobotTaskRepository
{
    public Task<bool> IsSimulationTaskAsync(Guid taskId, CancellationToken cancellationToken) =>
        db.JobTasks.AsNoTracking().AnyAsync(task => task.Id == taskId && task.RuntimeId == "sim", cancellationToken);
}
