using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class MissionDataRepository(LiBotDbContext db) : IMissionDataRepository
{
    public async Task<IReadOnlyList<Mission>> GetAllMissionsAsync(CancellationToken cancellationToken) =>
        await db.Jobs.AsNoTracking().OrderByDescending(item => item.CreatedAtUtc).ToListAsync(cancellationToken);

    public Task<Mission?> GetMissionAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.Jobs : db.Jobs.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<JobTask?> GetTaskAsync(Guid missionId, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.JobTasks : db.JobTasks.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.JobId == missionId, cancellationToken);
    }

    public Task<WorkflowDefinition?> GetWorkflowAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? db.Workflows : db.Workflows.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> MissionExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Jobs.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public Task<bool> WorkflowExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Workflows.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public Task<bool> RobotExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Robots.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public Task<string?> GetRobotNameAsync(Guid id, CancellationToken cancellationToken) =>
        db.Robots.AsNoTracking().Where(item => item.Id == id).Select(item => item.Name).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Maps.AsNoTracking().AnyAsync(item => item.Id == id, cancellationToken);

    public async Task CreateMissionAsync(Mission mission, Guid workflowId, Guid? robotId, string runtimeId,
        string? initialContext, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Jobs.AddAsync(mission, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var workflowTasks = await db.WorkflowTasks.Where(item => item.WorkflowId == workflowId)
            .OrderBy(item => item.SequenceNumber).ToListAsync(cancellationToken);
        var jobTask = new JobTask(mission.Id, robotId, runtimeId);
        await db.JobTasks.AddAsync(jobTask, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var taskIds = workflowTasks.Select(item => item.Id).ToArray();
        var steps = await db.TaskSteps.Where(item => taskIds.Contains(item.WorkflowTaskId)).ToListAsync(cancellationToken);
        var sequence = 1;
        foreach (var step in steps)
            await db.JobSteps.AddAsync(new JobStep(jobTask.Id, step.Id, sequence++), cancellationToken);
        if (!string.IsNullOrWhiteSpace(initialContext))
            await db.JobEvents.AddAsync(new JobEvent(mission.Id, "WORKFLOW_INITIAL_CONTEXT", initialContext), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowStepResponse>> GetWorkflowStepsAsync(Guid workflowId,
        CancellationToken cancellationToken)
    {
        var rows = await (from task in db.WorkflowTasks.AsNoTracking()
                          join step in db.TaskSteps.AsNoTracking() on task.Id equals step.WorkflowTaskId
                          where task.WorkflowId == workflowId
                          orderby task.SequenceNumber
                          select new { step.Id, task.SequenceNumber, step.StepType, task.Name, step.Configuration })
            .ToListAsync(cancellationToken);
        return rows.Select(row => new WorkflowStepResponse(row.Id, row.SequenceNumber, row.StepType, row.Name,
            ParseConfiguration(row.Configuration))).ToList();
    }

    public Task<string?> GetInitialContextAsync(Guid missionId, CancellationToken cancellationToken) =>
        db.JobEvents.AsNoTracking()
            .Where(item => item.JobId == missionId && item.EventType == "WORKFLOW_INITIAL_CONTEXT")
            .OrderByDescending(item => item.EventTimeUtc).Select(item => item.Data).FirstOrDefaultAsync(cancellationToken);

    public async Task<JobTask> CreateMissingJobTaskAsync(Guid missionId, Guid workflowId, string runtimeId,
        CancellationToken cancellationToken)
    {
        var workflowTaskIds = await db.WorkflowTasks.AsNoTracking().Where(item => item.WorkflowId == workflowId)
            .OrderBy(item => item.SequenceNumber).Select(item => item.Id).ToListAsync(cancellationToken);
        var workflowSteps = workflowTaskIds.Count == 0 ? [] : await db.TaskSteps.AsNoTracking()
            .Where(item => workflowTaskIds.Contains(item.WorkflowTaskId)).OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var jobTask = new JobTask(missionId, null, runtimeId);
        await db.JobTasks.AddAsync(jobTask, cancellationToken);
        var stepsByTask = workflowSteps.GroupBy(item => item.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var sequence = 1;
        foreach (var workflowTaskId in workflowTaskIds)
        {
            if (!stepsByTask.TryGetValue(workflowTaskId, out var taskSteps)) continue;
            foreach (var taskStep in taskSteps)
                await db.JobSteps.AddAsync(new JobStep(jobTask.Id, taskStep.Id, sequence++), cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        return jobTask;
    }

    public async Task<IReadOnlyList<string?>> GetWorkflowStepEventDataAsync(Guid missionId,
        CancellationToken cancellationToken) =>
        await db.JobEvents.AsNoTracking().Where(item => item.JobId == missionId && item.EventType == "WORKFLOW_STEP")
            .OrderBy(item => item.EventTimeUtc).Select(item => item.Data).ToListAsync(cancellationToken);

    public async Task AddWorkflowStepEventAsync(Guid missionId, string data, CancellationToken cancellationToken)
    {
        await db.JobEvents.AddAsync(new JobEvent(missionId, "WORKFLOW_STEP", data), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddJobTaskAsync(JobTask task, CancellationToken cancellationToken)
    {
        await db.JobTasks.AddAsync(task, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public async Task DeleteMissionAsync(Mission mission, CancellationToken cancellationToken)
    {
        db.Jobs.Remove(mission);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static System.Text.Json.JsonElement? ParseConfiguration(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return null;
        try { return JsonDocument.Parse(configuration).RootElement.Clone(); }
        catch (JsonException) { return JsonSerializer.SerializeToElement(configuration); }
    }
}
