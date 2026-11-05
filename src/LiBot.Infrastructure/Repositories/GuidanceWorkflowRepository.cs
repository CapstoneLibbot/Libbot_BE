using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LiBot.Infrastructure.Repositories;

public sealed class GuidanceWorkflowRepository(LiBotDbContext db) : IGuidanceWorkflowRepository
{
    public async Task<IReadOnlyList<GuidanceWorkflowSnapshot>> GetActiveWorkflowsAsync(
        string workflowType,
        CancellationToken cancellationToken)
    {
        var workflows = await db.Workflows
            .AsNoTracking()
            .Where(item => item.Type == workflowType && item.IsActive)
            .OrderBy(item => item.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        var snapshots = new List<GuidanceWorkflowSnapshot>(workflows.Count);
        foreach (var workflow in workflows)
        {
            var tasks = await db.WorkflowTasks
                .AsNoTracking()
                .Where(item => item.WorkflowId == workflow.Id)
                .OrderBy(item => item.SequenceNumber)
                .ToListAsync(cancellationToken);

            var taskIds = tasks.Select(item => item.Id).ToList();
            var steps = taskIds.Count == 0
                ? []
                : await db.TaskSteps
                    .AsNoTracking()
                    .Where(item => taskIds.Contains(item.WorkflowTaskId))
                    .ToListAsync(cancellationToken);
            var stepsByTask = steps
                .GroupBy(item => item.WorkflowTaskId)
                .ToDictionary(group => group.Key, group => group
                    .Select(step => new GuidanceTaskStepSnapshot(
                        step.Id, step.StepType, step.Configuration, step.CreatedAtUtc))
                    .ToList());

            snapshots.Add(new GuidanceWorkflowSnapshot(
                workflow.Id,
                workflow.Type,
                workflow.Name,
                tasks.Select(task => new GuidanceWorkflowTaskSnapshot(
                    task.Id,
                    task.Name,
                    task.SequenceNumber,
                    stepsByTask.GetValueOrDefault(task.Id) ?? []))
                    .ToList()));
        }

        return snapshots;
    }

    public async Task CreateExecutionAsync(
        Mission mission,
        JobTask jobTask,
        Guid workflowId,
        string initialContext,
        CancellationToken cancellationToken)
    {
        db.Jobs.Add(mission);
        db.JobTasks.Add(jobTask);

        var workflowTasks = await db.WorkflowTasks
            .AsNoTracking()
            .Where(task => task.WorkflowId == workflowId)
            .OrderBy(task => task.SequenceNumber)
            .ToListAsync(cancellationToken);
        var workflowTaskIds = workflowTasks.Select(task => task.Id).ToArray();
        var primaryWorkflowTask = workflowTasks.FirstOrDefault();
        jobTask.AssignWorkflowTask(primaryWorkflowTask?.Id, primaryWorkflowTask?.Name ?? mission.Name);
        var steps = workflowTaskIds.Length == 0
            ? []
            : await db.TaskSteps
                .AsNoTracking()
                .Where(step => workflowTaskIds.Contains(step.WorkflowTaskId))
                .OrderBy(step => step.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        var stepsByTask = steps.GroupBy(step => step.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var sequence = 1;
        foreach (var workflowTaskId in workflowTaskIds)
        {
            if (!stepsByTask.TryGetValue(workflowTaskId, out var taskSteps)) continue;
            foreach (var step in taskSteps)
                db.JobSteps.Add(new JobStep(jobTask.Id, step.Id, sequence++, step.StepType));
        }

        db.JobEvents.Add(new JobEvent(mission.Id, "WORKFLOW_INITIAL_CONTEXT", initialContext));
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Mission?> GetMissionAsync(Guid missionId, CancellationToken cancellationToken) =>
        db.Jobs.FirstOrDefaultAsync(job => job.Id == missionId, cancellationToken);

    public async Task<bool> CompleteExecutionAsync(
        Guid missionId,
        bool succeeded,
        string? message,
        CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(job => job.Id == missionId, cancellationToken);
        if (mission is null) return false;
        if (mission.Status is "Completed" or "Failed" or "Cancelled") return true;

        if (succeeded) mission.Complete();
        else mission.Fail();

        var status = succeeded ? "COMPLETED" : "FAILED";
        var tasks = await db.JobTasks.Where(task => task.JobId == missionId).ToListAsync(cancellationToken);
        foreach (var task in tasks)
        {
            task.UpdateExecutionState(JsonSerializer.Serialize(new
            {
                missionId,
                executionId = task.ExecutionId,
                runtimeId = task.RuntimeId,
                status,
                error = succeeded ? null : message,
                updatedAt = DateTime.UtcNow
            }));
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
