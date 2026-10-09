using System.Text.Json;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowController(LiBotDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkflowResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var workflows = await db.Workflows.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var result = new List<WorkflowResponse>();
        foreach (var workflow in workflows) result.Add(await ToResponse(workflow, cancellationToken));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await db.Workflows.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return workflow is null ? NotFound() : Ok(await ToResponse(workflow, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> Create(WorkflowWriteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Workflow name is required.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var workflow = new WorkflowDefinition(request.Name, request.Description ?? string.Empty);
        workflow.SetType(request.Type ?? "GENERAL");
        workflow.SetActive(request.IsActive ?? true);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceSteps(workflow.Id, request.Steps ?? [], cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, await ToResponse(workflow, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowResponse>> Update(Guid id, WorkflowWriteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Workflow name is required.");
        var workflow = await db.Workflows.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workflow is null) return NotFound();
        var currentTasks = await db.WorkflowTasks.Where(x => x.WorkflowId == id).ToListAsync(cancellationToken);
        var currentTaskIds = currentTasks.Select(x => x.Id).ToList();
        var currentStepIds = await db.TaskSteps.Where(x => currentTaskIds.Contains(x.WorkflowTaskId))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (await db.JobSteps.AnyAsync(x => currentStepIds.Contains(x.TaskStepId), cancellationToken))
            return Conflict("This workflow has mission history and its steps cannot be replaced.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        workflow.Update(request.Name, request.Description ?? string.Empty);
        workflow.SetType(request.Type ?? workflow.Type);
        workflow.SetActive(request.IsActive ?? workflow.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceSteps(id, request.Steps ?? [], cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(await ToResponse(workflow, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await db.Workflows.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workflow is null) return NotFound();
        var tasks = await db.WorkflowTasks.Where(x => x.WorkflowId == id).ToListAsync(cancellationToken);
        var taskIds = tasks.Select(x => x.Id).ToList();
        var stepIds = await db.TaskSteps.Where(x => taskIds.Contains(x.WorkflowTaskId)).Select(x => x.Id).ToListAsync(cancellationToken);
        if (await db.JobSteps.AnyAsync(x => stepIds.Contains(x.TaskStepId), cancellationToken))
            return Conflict("This workflow has mission history and cannot be deleted.");
        db.WorkflowTasks.RemoveRange(tasks);
        db.Workflows.Remove(workflow);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task ReplaceSteps(Guid workflowId, IReadOnlyList<WorkflowStepWriteRequest> steps,
        CancellationToken cancellationToken)
    {
        var currentTasks = await db.WorkflowTasks.Where(x => x.WorkflowId == workflowId).ToListAsync(cancellationToken);
        var currentTaskIds = currentTasks.Select(x => x.Id).ToList();
        var stepIds = await db.TaskSteps.Where(x => currentTaskIds.Contains(x.WorkflowTaskId)).Select(x => x.Id).ToListAsync(cancellationToken);
        if (await db.JobSteps.AnyAsync(x => stepIds.Contains(x.TaskStepId), cancellationToken))
            throw new InvalidOperationException("This workflow has mission history and its steps cannot be replaced.");
        db.WorkflowTasks.RemoveRange(currentTasks);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var step in steps.OrderBy(x => x.Sequence))
        {
            var task = new WorkflowTask(workflowId, step.Sequence, step.Name ?? step.Type);
            db.WorkflowTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            db.TaskSteps.Add(new TaskStep(task.Id, step.Type, step.Configuration?.GetRawText()));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<WorkflowResponse> ToResponse(WorkflowDefinition workflow, CancellationToken cancellationToken)
    {
        var tasks = await db.WorkflowTasks.AsNoTracking().Where(x => x.WorkflowId == workflow.Id)
            .OrderBy(x => x.SequenceNumber).ToListAsync(cancellationToken);
        var taskIds = tasks.Select(x => x.Id).ToList();
        var steps = await db.TaskSteps.AsNoTracking().Where(x => taskIds.Contains(x.WorkflowTaskId))
            .ToDictionaryAsync(x => x.WorkflowTaskId, cancellationToken);
        return new WorkflowResponse(workflow.Id, workflow.Name, workflow.Description, workflow.Type, workflow.IsActive,
            tasks.Select(task => steps.TryGetValue(task.Id, out var step)
                ? new WorkflowStepResponse(step.Id, task.SequenceNumber, step.StepType, task.Name, ParseConfiguration(step.Configuration))
                : null).Where(x => x is not null).Cast<WorkflowStepResponse>().ToList());
    }

    private static JsonElement? ParseConfiguration(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return null;
        try { return JsonDocument.Parse(configuration).RootElement.Clone(); }
        catch (JsonException) { return JsonSerializer.SerializeToElement(configuration); }
    }
}

public record WorkflowWriteRequest(string Name, string? Description, string? Type, bool? IsActive,
    IReadOnlyList<WorkflowStepWriteRequest>? Steps);
public record WorkflowStepWriteRequest(int Sequence, string Type, string? Name, JsonElement? Configuration);
public record WorkflowResponse(Guid Id, string Name, string Description, string Type, bool IsActive,
    IReadOnlyList<WorkflowStepResponse> Steps);
