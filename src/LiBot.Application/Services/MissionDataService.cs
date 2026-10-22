using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Services;

public sealed class MissionDataService(IMissionDataRepository repository) : IMissionDataService
{
    public Task<IReadOnlyList<Mission>> GetAllMissionsAsync(CancellationToken cancellationToken) => repository.GetAllMissionsAsync(cancellationToken);
    public Task<Mission?> GetMissionAsync(Guid id, bool tracking, CancellationToken cancellationToken) => repository.GetMissionAsync(id, tracking, cancellationToken);
    public Task<JobTask?> GetTaskAsync(Guid missionId, bool tracking, CancellationToken cancellationToken) => repository.GetTaskAsync(missionId, tracking, cancellationToken);
    public Task<WorkflowDefinition?> GetWorkflowAsync(Guid id, bool tracking, CancellationToken cancellationToken) => repository.GetWorkflowAsync(id, tracking, cancellationToken);
    public Task<bool> MissionExistsAsync(Guid id, CancellationToken cancellationToken) => repository.MissionExistsAsync(id, cancellationToken);
    public Task<bool> WorkflowExistsAsync(Guid id, CancellationToken cancellationToken) => repository.WorkflowExistsAsync(id, cancellationToken);
    public Task<bool> RobotExistsAsync(Guid id, CancellationToken cancellationToken) => repository.RobotExistsAsync(id, cancellationToken);
    public Task<string?> GetRobotNameAsync(Guid id, CancellationToken cancellationToken) => repository.GetRobotNameAsync(id, cancellationToken);
    public Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken) => repository.MapExistsAsync(id, cancellationToken);
    public Task CreateMissionAsync(Mission mission, Guid workflowId, Guid? robotId, string runtimeId, string? initialContext,
        CancellationToken cancellationToken) => repository.CreateMissionAsync(mission, workflowId, robotId, runtimeId, initialContext, cancellationToken);
    public Task<IReadOnlyList<WorkflowStepResponse>> GetWorkflowStepsAsync(Guid workflowId, CancellationToken cancellationToken) => repository.GetWorkflowStepsAsync(workflowId, cancellationToken);
    public Task<string?> GetInitialContextAsync(Guid missionId, CancellationToken cancellationToken) => repository.GetInitialContextAsync(missionId, cancellationToken);
    public Task<JobTask> CreateMissingJobTaskAsync(Guid missionId, Guid workflowId, string runtimeId, CancellationToken cancellationToken) =>
        repository.CreateMissingJobTaskAsync(missionId, workflowId, runtimeId, cancellationToken);
    public Task<IReadOnlyList<string?>> GetWorkflowStepEventDataAsync(Guid missionId, CancellationToken cancellationToken) =>
        repository.GetWorkflowStepEventDataAsync(missionId, cancellationToken);
    public Task AddWorkflowStepEventAsync(Guid missionId, string data, CancellationToken cancellationToken) =>
        repository.AddWorkflowStepEventAsync(missionId, data, cancellationToken);
    public Task AddJobTaskAsync(JobTask task, CancellationToken cancellationToken) => repository.AddJobTaskAsync(task, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => repository.SaveChangesAsync(cancellationToken);
    public Task DeleteMissionAsync(Mission mission, CancellationToken cancellationToken) => repository.DeleteMissionAsync(mission, cancellationToken);
}
