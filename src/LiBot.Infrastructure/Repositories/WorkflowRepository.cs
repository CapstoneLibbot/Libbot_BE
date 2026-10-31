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
        var steps = await db.TaskSteps.AsNoTracking().Where(item => taskIds.Contains(item.WorkflowTaskId))
            .ToDictionaryAsync(item => item.WorkflowTaskId, cancellationToken);
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
        var steps = await (tracking ? stepsQuery : stepsQuery.AsNoTracking())
            .ToDictionaryAsync(item => item.WorkflowTaskId, cancellationToken);
        return new WorkflowAggregate(workflow, tasks, steps);
    }

    public async Task<bool> CreateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowStepWriteRequest> steps,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceStepsAsync(workflow.Id, steps, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowStepWriteRequest> steps,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        if (!await CanReplaceStepsAsync(workflow.Id, cancellationToken)) return false;
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceStepsAsync(workflow.Id, steps, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var aggregate = await GetByIdAsync(id, tracking: true, cancellationToken);
        if (aggregate is null) return false;
        var stepIds = aggregate.Steps.Values.Select(item => item.Id).ToArray();
        if (await db.JobSteps.AnyAsync(item => stepIds.Contains(item.TaskStepId), cancellationToken)) return false;
        db.WorkflowTasks.RemoveRange(aggregate.Tasks);
        db.Workflows.Remove(aggregate.Workflow);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ReplaceStepsAsync(Guid workflowId, IReadOnlyList<WorkflowStepWriteRequest> steps,
        CancellationToken cancellationToken)
    {
        var currentTasks = await db.WorkflowTasks.Where(item => item.WorkflowId == workflowId).ToListAsync(cancellationToken);
        db.WorkflowTasks.RemoveRange(currentTasks);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var step in steps.OrderBy(item => item.Sequence))
        {
            var task = new WorkflowTask(workflowId, step.Sequence, step.Name ?? step.Type, step.Description);
            db.WorkflowTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            var configuration = step.Configuration?.GetRawText();
            if (!string.IsNullOrWhiteSpace(step.Id))
            {
                var configObject = string.IsNullOrWhiteSpace(configuration)
                    ? new JsonObject()
                    : JsonNode.Parse(configuration) as JsonObject ?? new JsonObject();
                configObject["id"] ??= step.Id;
                configuration = configObject.ToJsonString();
            }
            db.TaskSteps.Add(new TaskStep(task.Id, step.Type, configuration));
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
