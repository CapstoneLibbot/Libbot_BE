using LiBot.Application.Models;
using LiBot.Domain.Job;

namespace LiBot.Application.Interfaces;

public interface IGuidanceWorkflowRepository
{
    Task<IReadOnlyList<GuidanceWorkflowSnapshot>> GetActiveWorkflowsAsync(
        string workflowType,
        CancellationToken cancellationToken);

    Task CreateExecutionAsync(
        Job job,
        JobTask jobTask,
        RobotWorkflowDocument workflowDocument,
        string initialContext,
        CancellationToken cancellationToken);

    Task<Job?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
