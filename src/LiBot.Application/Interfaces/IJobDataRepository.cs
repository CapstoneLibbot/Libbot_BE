using LiBot.Application.Models;
using LiBot.Domain.Job;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Interfaces;

public interface IJobDataRepository
{
    Task<IReadOnlyList<Job>> GetAllJobsAsync(CancellationToken cancellationToken);
    Task<Job?> GetJobAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<JobTask?> GetTaskAsync(Guid jobId, bool tracking, CancellationToken cancellationToken);
    Task<WorkflowDefinition?> GetWorkflowAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> JobExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> WorkflowExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> RobotExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> GetRobotIdByMapAsync(Guid mapId, CancellationToken cancellationToken);
    Task<Guid> GetOrCreateRobotIdForMapAsync(Guid mapId, string runtimeId, string robotName,
        CancellationToken cancellationToken);
    Task<string?> GetRobotNameAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken);
    Task CreateJobAsync(Job job, RobotWorkflowDocument workflowDocument, string runtimeId,
        string? initialContext, CancellationToken cancellationToken);
    Task<RobotWorkflowDocument?> GetRobotWorkflowDocumentAsync(Guid jobId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkflowStepResponse>> GetWorkflowStepsAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<string?> GetInitialContextAsync(Guid jobId, CancellationToken cancellationToken);
    Task<JobTask> CreateMissingJobTaskAsync(Guid jobId, Guid workflowId, string runtimeId,
        CancellationToken cancellationToken);
    Task<Guid?> GetPrimaryWorkflowTaskIdAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string?>> GetWorkflowStepEventDataAsync(Guid jobId, CancellationToken cancellationToken);
    Task<bool> RecordWorkflowStepResultAsync(Guid jobId, string stepId, string status, string data,
        CancellationToken cancellationToken);
    Task PrepareJobForRetryAsync(Guid jobId, CancellationToken cancellationToken);
    Task SetJobTaskStatusesAsync(Guid jobId, string status, CancellationToken cancellationToken);
    Task AddJobTaskAsync(JobTask task, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteJobAsync(Job job, CancellationToken cancellationToken);
}
