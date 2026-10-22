using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

        var workflowTaskIds = await db.WorkflowTasks
            .AsNoTracking()
            .Where(task => task.WorkflowId == workflowId)
            .OrderBy(task => task.SequenceNumber)
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
        var steps = workflowTaskIds.Count == 0
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
                db.JobSteps.Add(new JobStep(jobTask.Id, step.Id, sequence++));
        }

        db.JobEvents.Add(new JobEvent(mission.Id, "WORKFLOW_INITIAL_CONTEXT", initialContext));
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Mission?> GetMissionAsync(Guid missionId, CancellationToken cancellationToken) =>
        db.Jobs.FirstOrDefaultAsync(job => job.Id == missionId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
