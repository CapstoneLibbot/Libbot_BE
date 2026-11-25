using System.Text.Json.Nodes;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Workflow;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class WorkflowRepository(LiBotDbContext db) : IWorkflowRepository
{
    public async Task<IReadOnlyList<WorkflowAggregate>> GetAllAsync(CancellationToken cancellationToken)
    {
        var workflows = await db.Workflows.AsNoTracking().OrderBy(item => item.Name).ToListAsync(cancellationToken);
        var ids = workflows.Select(item => item.Id).ToArray();
        var tasks = await db.WorkflowTasks.AsNoTracking().Where(item => ids.Contains(item.WorkflowId))
            .OrderBy(item => item.SequenceNumber).ToListAsync(cancellationToken);
        var taskIds = tasks.Select(item => item.Id).ToArray();
        var steps = (await db.TaskSteps.AsNoTracking().Where(item => taskIds.Contains(item.WorkflowTaskId))
                .OrderBy(item => item.SequenceNo).ToListAsync(cancellationToken))
            .GroupBy(item => item.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TaskStep>)group.ToList());
        return workflows.Select(workflow => new WorkflowAggregate(workflow,
            tasks.Where(task => task.WorkflowId == workflow.Id).ToList(), steps)).ToList();
    }

    public async Task<WorkflowAggregate?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var workflow = tracking
            ? await db.Workflows.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            : await db.Workflows.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workflow is null) return null;
        var tasksQuery = db.WorkflowTasks.Where(item => item.WorkflowId == id);
        var tasks = await (tracking ? tasksQuery : tasksQuery.AsNoTracking())
            .OrderBy(item => item.SequenceNumber).ToListAsync(cancellationToken);
        var taskIds = tasks.Select(item => item.Id).ToArray();
        var stepsQuery = db.TaskSteps.Where(item => taskIds.Contains(item.WorkflowTaskId));
        var stepRows = await (tracking ? stepsQuery : stepsQuery.AsNoTracking())
            .OrderBy(item => item.SequenceNo).ToListAsync(cancellationToken);
        var steps = stepRows.GroupBy(item => item.WorkflowTaskId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TaskStep>)group.ToList());
        return new WorkflowAggregate(workflow, tasks, steps);
    }

    public async Task<bool> CreateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowTaskWriteRequest> tasks,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceStepsAsync(workflow.Id, tasks, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowTaskWriteRequest> tasks,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await CanReplaceStepsAsync(workflow.Id, cancellationToken)) return false;
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceStepsAsync(workflow.Id, tasks, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var workflow = await db.Workflows.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workflow is null) return false;

        var activeWorkflows = await db.Workflows
            .Where(item => item.Type == workflow.Type && item.Status == Domain.Enums.WorkflowStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var activeWorkflow in activeWorkflows)
            if (activeWorkflow.Id != workflow.Id)
                activeWorkflow.SetActive(false);

        workflow.SetActive(true);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, LiBot.Domain.Enums.WorkflowStatus status,
        CancellationToken cancellationToken)
    {
        var workflow = await db.Workflows.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (workflow is null) return false;
        workflow.SetStatus(status);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var aggregate = await GetByIdAsync(id, tracking: true, cancellationToken);
        if (aggregate is null) return false;
        var stepIds = aggregate.Steps.Values.SelectMany(item => item).Select(item => item.Id).ToArray();
        if (await db.JobSteps.AnyAsync(item => stepIds.Contains(item.TaskStepId), cancellationToken)) return false;
        db.WorkflowTasks.RemoveRange(aggregate.Tasks);
        db.Workflows.Remove(aggregate.Workflow);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ReplaceStepsAsync(Guid workflowId, IReadOnlyList<WorkflowTaskWriteRequest> tasks,
        CancellationToken cancellationToken)
    {
        var currentTasks = await db.WorkflowTasks.Where(item => item.WorkflowId == workflowId).ToListAsync(cancellationToken);
        db.WorkflowTasks.RemoveRange(currentTasks);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var taskDraft in tasks.OrderBy(item => item.Sequence))
        {
            var task = new WorkflowTask(workflowId, taskDraft.Sequence, taskDraft.Name, taskDraft.Description);
            db.WorkflowTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            foreach (var step in taskDraft.Steps.OrderBy(item => item.Sequence))
            {
                var configuration = step.Configuration?.GetRawText();
                var configObject = string.IsNullOrWhiteSpace(configuration)
                    ? new JsonObject()
                    : JsonNode.Parse(configuration) as JsonObject
                      ?? throw new InvalidOperationException($"Step '{step.Name}' configuration must be a JSON object.");
                configObject["id"] ??= string.IsNullOrWhiteSpace(step.Id) ? Guid.NewGuid().ToString("N") : step.Id;
                configObject["name"] ??= step.Name;
                configObject["type"] ??= step.Type;
                configuration = configObject.ToJsonString();
                db.TaskSteps.Add(new TaskStep(task.Id, step.Type, configuration, step.Sequence, step.Description));
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> CanReplaceStepsAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var taskIds = await db.WorkflowTasks.Where(item => item.WorkflowId == workflowId)
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        var stepIds = await db.TaskSteps.Where(item => taskIds.Contains(item.WorkflowTaskId))
            .Select(item => item.Id).ToArrayAsync(cancellationToken);
        return !await db.JobSteps.AnyAsync(item => stepIds.Contains(item.TaskStepId), cancellationToken);
    }
}
