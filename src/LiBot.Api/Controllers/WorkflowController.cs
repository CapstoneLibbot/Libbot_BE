using System.Text.Json;
using System.Text.Json.Nodes;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflows")]
public class WorkflowController(LiBotDbContext db, IWorkflowDocumentService documents) : ControllerBase
{
    [HttpGet("templates")]
    public ActionResult<IReadOnlyList<WorkflowTemplateResponse>> GetTemplates() => Ok(documents.GetTemplates());

    [HttpPost("templates/{templateId}/create")]
    public async Task<ActionResult<WorkflowResponse>> CreateFromTemplate(
        string templateId,
        WorkflowTemplateCreateRequest? request,
        CancellationToken cancellationToken)
    {
        var template = documents.GetTemplate(templateId);
        if (template is null) return NotFound(new { message = "Workflow template not found." });
        var templateSteps = template.Steps.Select(step =>
            new WorkflowStepWriteRequest(step.Sequence, step.Type, step.Name, step.Configuration, step.Name)).ToList();
        var validationError = documents.ValidateSteps(templateSteps);
        if (validationError is not null) return BadRequest(new { message = validationError });
        var workflow = new WorkflowDefinition(
            string.IsNullOrWhiteSpace(request?.Name) ? template.Name : request.Name,
            template.Description);
        workflow.SetType(template.Type);
        workflow.SetActive(true);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Workflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceSteps(workflow.Id, templateSteps, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, await ToResponse(workflow, cancellationToken));
    }

    [HttpGet("{id:guid}/robot-json")]
    public async Task<IActionResult> GetRobotJson(
        Guid id,
        [FromQuery] string runtimeId = "real",
        CancellationToken cancellationToken = default)
    {
        if (runtimeId is not ("sim" or "real"))
            return BadRequest(new { message = "runtimeId must be 'sim' or 'real'." });
        try
        {
            var document = await documents.BuildRobotDocumentAsync(id, runtimeId, cancellationToken);
            return document is null ? NotFound() : Ok(document);
        }
        catch (InvalidWorkflowDefinitionException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

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
        var validationError = documents.ValidateSteps(request.Steps ?? []);
        if (validationError is not null) return BadRequest(new { message = validationError });
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
        var validationError = documents.ValidateSteps(request.Steps ?? []);
        if (validationError is not null) return BadRequest(new { message = validationError });
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
