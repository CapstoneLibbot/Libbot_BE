using LiBot.Application.Interfaces;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class RobotTaskRepository(LiBotDbContext db) : IRobotTaskRepository
{
    public async Task<bool> IsSimulationTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taskId, cancellationToken);
        return string.Equals(task?.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase);
    }
}
