using LiBot.Application.Models;
using LiBot.Domain.Mission;

namespace LiBot.Application.Interfaces;

public interface IGuidanceWorkflowRepository
{
    Task<IReadOnlyList<GuidanceWorkflowSnapshot>> GetActiveWorkflowsAsync(
        string workflowType,
        CancellationToken cancellationToken);

    Task CreateExecutionAsync(
        Mission mission,
        JobTask jobTask,
        Guid workflowId,
        string initialContext,
        CancellationToken cancellationToken);

    Task<Mission?> GetMissionAsync(Guid missionId, CancellationToken cancellationToken);
    Task<bool> CompleteExecutionAsync(Guid missionId, bool succeeded, string? message, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
