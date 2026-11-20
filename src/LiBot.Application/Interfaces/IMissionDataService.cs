using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Interfaces;

public interface IMissionDataService
{
    Task<IReadOnlyList<Mission>> GetAllMissionsAsync(CancellationToken cancellationToken);
    Task<Mission?> GetMissionAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<JobTask?> GetTaskAsync(Guid missionId, bool tracking, CancellationToken cancellationToken);
    Task<WorkflowDefinition?> GetWorkflowAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> MissionExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> WorkflowExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> RobotExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<string?> GetRobotNameAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken);
    Task CreateMissionAsync(Mission mission, Guid workflowId, string runtimeId,
        string? initialContext, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowStepResponse>> GetWorkflowStepsAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<string?> GetInitialContextAsync(Guid missionId, CancellationToken cancellationToken);
    Task<JobTask> CreateMissingJobTaskAsync(Guid missionId, Guid workflowId, string runtimeId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<string?>> GetWorkflowStepEventDataAsync(Guid missionId, CancellationToken cancellationToken);
    Task AddWorkflowStepEventAsync(Guid missionId, string data, CancellationToken cancellationToken);
    Task AddJobTaskAsync(JobTask task, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteMissionAsync(Mission mission, CancellationToken cancellationToken);
}
