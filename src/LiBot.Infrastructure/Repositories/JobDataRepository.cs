using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Job;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RobotEntity = LiBot.Domain.Robot.Robot;

namespace LiBot.Infrastructure.Repositories;

public sealed class JobDataRepository(LiBotDbContext db) : IJobDataRepository
{
    public async Task<IReadOnlyList<Job>> GetAllJobsAsync(CancellationToken cancellationToken) =>
        await db.Jobs.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<Job?> GetJobAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.Jobs : db.Jobs.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<JobTask?> GetTaskAsync(Guid jobId, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.JobTasks : db.JobTasks.AsNoTracking();
        var task = await query.Where(item => item.JobId == jobId).OrderBy(item => item.SequenceNo)
            .FirstOrDefaultAsync(cancellationToken);
        if (task is not null)
            await JobEventPersistence.HydrateJobTaskAsync(db, task, cancellationToken);
        return task;
    }

    public Task<WorkflowDefinition?> GetWorkflowAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.Workflows : db.Workflows.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> JobExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Jobs.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public Task<bool> WorkflowExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Workflows.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public Task<bool> RobotExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Robots.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public Task<Guid?> GetRobotIdByMapAsync(Guid mapId, CancellationToken cancellationToken) =>
        db.Robots.AsNoTracking().Where(item => item.MapId == mapId)
            .OrderBy(item => item.Id).Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid> GetOrCreateRobotIdForMapAsync(Guid mapId, string runtimeId, string robotName,
        CancellationToken cancellationToken)
    {
        var existingId = await GetRobotIdByMapAsync(mapId, cancellationToken);
        if (existingId.HasValue)
            return existingId.Value;

        var serialNumber = $"RUNTIME-{runtimeId.Trim().ToUpperInvariant()}";
        var robot = await db.Robots.FirstOrDefaultAsync(
            item => item.SerialNumber == serialNumber,
            cancellationToken);
        if (robot is not null)
        {
            robot.AssignMap(mapId);
        }
        else
        {
            robot = new RobotEntity(robotName, serialNumber, mapId);
            db.Robots.Add(robot);
        }

        await db.SaveChangesAsync(cancellationToken);
        return robot.Id;
    }

    public Task<string?> GetRobotNameAsync(Guid id, CancellationToken cancellationToken) =>
        db.Robots.AsNoTracking().Where(item => item.Id == id).Select(item => item.Name).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Maps.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public async Task CreateJobAsync(Job job, RobotWorkflowDocument workflowDocument, string runtimeId,
        string? initialContext, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (job.WorkflowId != workflowDocument.WorkflowId)
            throw new InvalidOperationException("Job workflow id must match the workflow being created.");
        if (!string.Equals(workflowDocument.RuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Job runtime id must match the workflow document runtime id.");
        if (workflowDocument.Tasks.Count == 0 || workflowDocument.Tasks.Any(task => task.Steps.Count == 0))
            throw new InvalidOperationException("A job workflow must contain at least one step in every task.");
        await db.Jobs.AddAsync(job, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var taskDocument in workflowDocument.Tasks.OrderBy(item => item.Sequence))
        {
            var jobTask = new JobTask(job.Id, taskDocument.WorkflowTaskId, taskDocument.Sequence,
                taskDocument.Name, runtimeId);
            await db.JobTasks.AddAsync(jobTask, cancellationToken);
            foreach (var step in taskDocument.Steps.OrderBy(item => item.Sequence))
                await db.JobSteps.AddAsync(new JobStep(jobTask.Id, step.TaskStepId, step.Sequence, step.Type,
                    step.Configuration.GetRawText()), cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(initialContext)
            && !string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            await db.JobEvents.AddAsync(await JobEventPersistence.CreateForJobAsync(
                db, job.Id, "WORKFLOW_INITIAL_CONTEXT", initialContext, cancellationToken), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<RobotWorkflowDocument?> GetRobotWorkflowDocumentAsync(Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        if (job is null) return null;
        var workflow = await db.Workflows.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == job.WorkflowId, cancellationToken);
        if (workflow is null) return null;
        var jobTasks = await db.JobTasks.AsNoTracking().Where(item => item.JobId == jobId)
            .OrderBy(item => item.SequenceNo).ToListAsync(cancellationToken);
        if (jobTasks.Count == 0) return null;

        var jobTaskIds = jobTasks.Select(item => item.Id).ToArray();
        var jobSteps = await db.JobSteps.AsNoTracking().Where(item => jobTaskIds.Contains(item.JobTaskId))
            .OrderBy(item => item.Sequence).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        var stepsByTask = jobSteps.GroupBy(item => item.JobTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var taskDocuments = new List<RobotWorkflowTaskDocument>(jobTasks.Count);
        foreach (var jobTask in jobTasks)
        {
            if (!stepsByTask.TryGetValue(jobTask.Id, out var steps) || steps.Count == 0)
                return null;
            var stepDocuments = new List<RobotWorkflowStepDocument>(steps.Count);
            foreach (var step in steps)
            {
                if (string.IsNullOrWhiteSpace(step.Configuration)) return null;
                try
                {
                    using var configuration = JsonDocument.Parse(step.Configuration);
                    if (configuration.RootElement.ValueKind != JsonValueKind.Object) return null;
                    stepDocuments.Add(new RobotWorkflowStepDocument(step.TaskStepId, step.Sequence, step.StepType,
                        configuration.RootElement.Clone()));
                }
                catch (JsonException)
                {
                    return null;
                }
            }
            taskDocuments.Add(new RobotWorkflowTaskDocument(jobTask.WorkflowTaskId, jobTask.SequenceNo,
                jobTask.Name, jobTask.Description, stepDocuments));
        }

        return new RobotWorkflowDocument(1, workflow.Id, workflow.Name, workflow.Type,
            jobTasks[0].RuntimeId, taskDocuments);
    }

    public async Task<IReadOnlyList<WorkflowStepResponse>> GetWorkflowStepsAsync(Guid workflowId,
        CancellationToken cancellationToken)
    {
        var rows = await (from task in db.WorkflowTasks.AsNoTracking()
                          join step in db.TaskSteps.AsNoTracking() on task.Id equals step.WorkflowTaskId
                          where task.WorkflowId == workflowId
            orderby task.SequenceNumber, step.SequenceNo
            select new { step.Id, task.SequenceNumber, step.SequenceNo, step.StepType, task.Name, task.Description, step.Configuration })
            .ToListAsync(cancellationToken);
        return rows.Select((row, index) => new WorkflowStepResponse(row.Id, index + 1, row.StepType,
            ReadStepName(row.Configuration) ?? row.Name, ParseConfiguration(row.Configuration), row.Description)).ToList();
    }

    public async Task<string?> GetInitialContextAsync(Guid jobId, CancellationToken cancellationToken) =>
        (await JobEventPersistence.GetEventDataAsync(db, jobId, "WORKFLOW_INITIAL_CONTEXT", cancellationToken))
            .LastOrDefault();

    public async Task<JobTask> CreateMissingJobTaskAsync(Guid jobId, Guid workflowId, string runtimeId,
        CancellationToken cancellationToken)
    {
        var workflowTasks = await db.WorkflowTasks.AsNoTracking().Where(item => item.WorkflowId == workflowId)
            .OrderBy(item => item.SequenceNumber).ToListAsync(cancellationToken);
        var workflowTaskIds = workflowTasks.Select(item => item.Id).ToArray();
        var workflowSteps = workflowTaskIds.Length == 0 ? [] : await db.TaskSteps.AsNoTracking()
            .Where(item => workflowTaskIds.Contains(item.WorkflowTaskId)).OrderBy(item => item.WorkflowTaskId)
            .ThenBy(item => item.SequenceNo)
            .ToListAsync(cancellationToken);
        var primaryWorkflowTask = workflowTasks.FirstOrDefault();
        if (primaryWorkflowTask is null)
            throw new InvalidOperationException("A job workflow must contain at least one task.");
        var stepsByTask = workflowSteps.GroupBy(item => item.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());
        JobTask? primaryJobTask = null;
        for (var taskIndex = 0; taskIndex < workflowTasks.Count; taskIndex++)
        {
            var workflowTask = workflowTasks[taskIndex];
            var workflowTaskId = workflowTask.Id;
            if (!stepsByTask.TryGetValue(workflowTaskId, out var taskSteps)) continue;
            var jobTask = new JobTask(jobId, workflowTask.Id, workflowTask.SequenceNumber, workflowTask.Name, runtimeId);
            primaryJobTask ??= jobTask;
            await db.JobTasks.AddAsync(jobTask, cancellationToken);
            foreach (var taskStep in taskSteps)
            {
                var configuration = CompleteLegacyConfiguration(taskStep.Configuration, workflowTask.Name,
                    taskStep.StepType, taskStep.Id);
                await db.JobSteps.AddAsync(new JobStep(jobTask.Id, taskStep.Id, taskStep.SequenceNo,
                    taskStep.StepType, configuration), cancellationToken);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return primaryJobTask ?? throw new InvalidOperationException("The job workflow contains no configured steps.");
    }

    public Task<Guid?> GetPrimaryWorkflowTaskIdAsync(Guid workflowId, CancellationToken cancellationToken) =>
        db.WorkflowTasks.AsNoTracking().Where(item => item.WorkflowId == workflowId)
            .OrderBy(item => item.SequenceNumber).Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<string?>> GetWorkflowStepEventDataAsync(Guid jobId,
        CancellationToken cancellationToken) =>
        await JobEventPersistence.GetEventDataAsync(db, jobId, "WORKFLOW_STEP", cancellationToken);

    public async Task<bool> RecordWorkflowStepResultAsync(Guid jobId, string stepId, string status, string data,
        CancellationToken cancellationToken)
    {
        var steps = await (from step in db.JobSteps
                           join jobTaskRow in db.JobTasks on step.JobTaskId equals jobTaskRow.Id
                           where jobTaskRow.JobId == jobId
                           select step).ToListAsync(cancellationToken);
        var jobStep = steps.FirstOrDefault(step => ReadStepId(step.Configuration) == stepId);
        if (jobStep is null)
        {
            if (stepId.StartsWith("__inventory_scan", StringComparison.Ordinal)) return false;
            throw new InvalidOperationException($"Job '{jobId}' has no step with id '{stepId}'.");
        }

        jobStep.UpdateStatus(status.Equals("STEP_FAILED", StringComparison.OrdinalIgnoreCase) ? "Failed" : "Completed");
        var task = await db.JobTasks.FirstAsync(item => item.Id == jobStep.JobTaskId, cancellationToken);
        var taskSteps = await db.JobSteps.Where(item => item.JobTaskId == task.Id).ToListAsync(cancellationToken);
        if (taskSteps.Any(item => item.Status == "Failed")) task.UpdateStatus("Failed");
        else if (taskSteps.All(item => item.Status == "Completed")) task.UpdateStatus("Completed");
        else task.UpdateStatus("Running");
        db.JobEvents.Add(JobEventPersistence.CreateForJobStep(jobStep.Id, "WORKFLOW_STEP", data));
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task PrepareJobForRetryAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var tasks = await db.JobTasks.Where(item => item.JobId == jobId).ToListAsync(cancellationToken);
        foreach (var task in tasks) task.PrepareForRetry();
        var taskIds = tasks.Select(item => item.Id).ToArray();
        var steps = await db.JobSteps.Where(item => taskIds.Contains(item.JobTaskId)).ToListAsync(cancellationToken);
        foreach (var step in steps) step.PrepareForRetry();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetJobTaskStatusesAsync(Guid jobId, string status, CancellationToken cancellationToken)
    {
        var tasks = await db.JobTasks.Where(item => item.JobId == jobId).ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            if (status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                || task.Status is "Pending" or "Running")
                task.UpdateStatus(status);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddJobTaskAsync(JobTask task, CancellationToken cancellationToken)
    {
        await db.JobTasks.AddAsync(task, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task DeleteJobAsync(Job job, CancellationToken cancellationToken)
    {
        var jobTaskIds = await db.JobTasks.Where(item => item.JobId == job.Id)
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var jobStepIds = await db.JobSteps.Where(item => jobTaskIds.Contains(item.JobTaskId))
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var inventoryReportIds = await db.InventoryReports.Where(item => item.JobId == job.Id)
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        db.JobEvents.RemoveRange(db.JobEvents.Where(item => jobStepIds.Contains(item.JobStepId)));
        db.InventoryReportDetails.RemoveRange(db.InventoryReportDetails.Where(item => inventoryReportIds.Contains(item.ReportId)));
        db.InventoryReports.RemoveRange(db.InventoryReports.Where(item => item.JobId == job.Id));
        db.PatrolReports.RemoveRange(db.PatrolReports.Where(item => item.JobId == job.Id));
        db.Feedback.RemoveRange(db.Feedback.Where(item => item.JobId == job.Id));
        db.JobSteps.RemoveRange(db.JobSteps.Where(item => jobTaskIds.Contains(item.JobTaskId)));
        db.JobTasks.RemoveRange(db.JobTasks.Where(item => item.JobId == job.Id));
        db.Jobs.Remove(job);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static System.Text.Json.JsonElement? ParseConfiguration(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return null;
        try { return JsonDocument.Parse(configuration).RootElement.Clone(); }
        catch (JsonException) { return JsonSerializer.SerializeToElement(configuration); }
    }

    private static string? ReadStepName(string? configuration)
    {
        var parsed = ParseConfiguration(configuration);
        return parsed is { ValueKind: JsonValueKind.Object } value
            && value.TryGetProperty("name", out var name)
            && name.ValueKind == JsonValueKind.String
                ? name.GetString()
                : null;
    }

    private static string? ReadStepId(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return null;
        try
        {
            using var document = JsonDocument.Parse(configuration);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("id", out var id)
                && id.ValueKind == JsonValueKind.String
                    ? id.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string CompleteLegacyConfiguration(string? configuration, string taskName, string stepType, Guid stepId)
    {
        var root = string.IsNullOrWhiteSpace(configuration)
            ? new System.Text.Json.Nodes.JsonObject()
            : System.Text.Json.Nodes.JsonNode.Parse(configuration) as System.Text.Json.Nodes.JsonObject
              ?? new System.Text.Json.Nodes.JsonObject();
        root["id"] ??= stepId.ToString("N");
        root["name"] ??= taskName;
        root["type"] ??= stepType;
        return root.ToJsonString();
    }
}
