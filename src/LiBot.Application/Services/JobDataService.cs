using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Job;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Services;

public sealed class JobDataService(IJobDataRepository repository) : IJobDataService
{
    public Task<IReadOnlyList<Job>> GetAllJobsAsync(CancellationToken cancellationToken) => repository.GetAllJobsAsync(cancellationToken);
    public Task<Job?> GetJobAsync(Guid id, bool tracking, CancellationToken cancellationToken) => repository.GetJobAsync(id, tracking, cancellationToken);
    public Task<JobTask?> GetTaskAsync(Guid jobId, bool tracking, CancellationToken cancellationToken) => repository.GetTaskAsync(jobId, tracking, cancellationToken);
    public Task<WorkflowDefinition?> GetWorkflowAsync(Guid id, bool tracking, CancellationToken cancellationToken) => repository.GetWorkflowAsync(id, tracking, cancellationToken);
    public Task<bool> JobExistsAsync(Guid id, CancellationToken cancellationToken) => repository.JobExistsAsync(id, cancellationToken);
    public Task<bool> WorkflowExistsAsync(Guid id, CancellationToken cancellationToken) => repository.WorkflowExistsAsync(id, cancellationToken);
    public Task<bool> RobotExistsAsync(Guid id, CancellationToken cancellationToken) => repository.RobotExistsAsync(id, cancellationToken);
    public Task<Guid> GetOrCreateRobotIdForMapAsync(Guid mapId, string runtimeId, string robotName,
        CancellationToken cancellationToken) =>
        repository.GetOrCreateRobotIdForMapAsync(mapId, runtimeId, robotName, cancellationToken);
    public Task<string?> GetRobotNameAsync(Guid id, CancellationToken cancellationToken) => repository.GetRobotNameAsync(id, cancellationToken);
    public Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken) => repository.MapExistsAsync(id, cancellationToken);
    public Task CreateJobAsync(Job job, RobotWorkflowDocument workflowDocument, string runtimeId, string? initialContext,
        CancellationToken cancellationToken) => repository.CreateJobAsync(job, workflowDocument, runtimeId, initialContext, cancellationToken);
    public Task<RobotWorkflowDocument?> GetRobotWorkflowDocumentAsync(Guid jobId, CancellationToken cancellationToken) =>
        repository.GetRobotWorkflowDocumentAsync(jobId, cancellationToken);
    public Task<IReadOnlyList<WorkflowStepResponse>> GetWorkflowStepsAsync(Guid workflowId, CancellationToken cancellationToken) => repository.GetWorkflowStepsAsync(workflowId, cancellationToken);
    public Task<string?> GetInitialContextAsync(Guid jobId, CancellationToken cancellationToken) => repository.GetInitialContextAsync(jobId, cancellationToken);
    public Task<JobTask> CreateMissingJobTaskAsync(Guid jobId, Guid workflowId, string runtimeId, CancellationToken cancellationToken) =>
        repository.CreateMissingJobTaskAsync(jobId, workflowId, runtimeId, cancellationToken);
    public Task<IReadOnlyList<string?>> GetWorkflowStepEventDataAsync(Guid jobId, CancellationToken cancellationToken) =>
        repository.GetWorkflowStepEventDataAsync(jobId, cancellationToken);
    public Task<bool> RecordWorkflowStepResultAsync(Guid jobId, string stepId, string status, string data,
        CancellationToken cancellationToken) =>
        repository.RecordWorkflowStepResultAsync(jobId, stepId, status, data, cancellationToken);
    public Task PrepareJobForRetryAsync(Guid jobId, CancellationToken cancellationToken) =>
        repository.PrepareJobForRetryAsync(jobId, cancellationToken);
    public Task SetJobTaskStatusesAsync(Guid jobId, string status, CancellationToken cancellationToken) =>
        repository.SetJobTaskStatusesAsync(jobId, status, cancellationToken);
    public Task AddJobTaskAsync(JobTask task, CancellationToken cancellationToken) => repository.AddJobTaskAsync(task, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken) => repository.SaveChangesAsync(cancellationToken);
    public Task DeleteJobAsync(Job job, CancellationToken cancellationToken) => repository.DeleteJobAsync(job, cancellationToken);
}
