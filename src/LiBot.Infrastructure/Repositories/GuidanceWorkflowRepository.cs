using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Enums;
using LiBot.Domain.Job;
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
            .Where(item => item.Type == workflowType && item.Status == WorkflowStatus.Active)
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
                    .OrderBy(item => item.WorkflowTaskId)
                    .ThenBy(item => item.SequenceNo)
                    .ToListAsync(cancellationToken);
            var stepsByTask = steps
                .GroupBy(item => item.WorkflowTaskId)
                .ToDictionary(group => group.Key, group => group
                    .Select(step => new GuidanceTaskStepSnapshot(
                        step.Id, step.SequenceNo, step.StepType, step.Configuration, step.CreatedAtUtc))
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
        Job job,
        JobTask jobTask,
        RobotWorkflowDocument workflowDocument,
        string initialContext,
        CancellationToken cancellationToken)
    {
        db.Jobs.Add(job);
        if (job.WorkflowId != workflowDocument.WorkflowId)
            throw new InvalidOperationException("Job workflow id does not match the execution workflow id.");
        if (workflowDocument.Tasks.Count == 0 || workflowDocument.Tasks.Any(task => task.Steps.Count == 0))
            throw new InvalidOperationException("A guidance workflow must have at least one step in every task.");

        foreach (var taskDocument in workflowDocument.Tasks.OrderBy(item => item.Sequence))
        {
            var task = taskDocument.WorkflowTaskId == jobTask.WorkflowTaskId
                ? jobTask
                : new JobTask(job.Id, taskDocument.WorkflowTaskId, taskDocument.Sequence,
                    taskDocument.Name, workflowDocument.RuntimeId);
            if (task.WorkflowTaskId != taskDocument.WorkflowTaskId
                || task.SequenceNo != taskDocument.Sequence
                || !string.Equals(task.Name, taskDocument.Name, StringComparison.Ordinal))
                throw new InvalidOperationException("The primary job task does not match its workflow task.");
            db.JobTasks.Add(task);
            foreach (var step in taskDocument.Steps.OrderBy(item => item.Sequence))
                db.JobSteps.Add(new JobStep(task.Id, step.TaskStepId, step.Sequence, step.Type,
                    step.Configuration.GetRawText()));
        }

        await db.SaveChangesAsync(cancellationToken);
        if (!string.Equals(workflowDocument.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
        {
            db.JobEvents.Add(await JobEventPersistence.CreateForJobAsync(
                db, job.Id, "WORKFLOW_INITIAL_CONTEXT", initialContext, cancellationToken));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<Job?> GetJobAsync(Guid jobId, CancellationToken cancellationToken) =>
        db.Jobs.FirstOrDefaultAsync(job => job.Id == jobId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
