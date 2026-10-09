using System.Text.Json;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/missions")]
public class MissionController(LiBotDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MissionResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var missions = await db.Jobs.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        var responses = new List<MissionResponse>();
        foreach (var mission in missions)
        {
            var task = await db.JobTasks.AsNoTracking().FirstOrDefaultAsync(x => x.JobId == mission.Id, cancellationToken);
            var robotName = task?.RobotId is Guid robotId
                ? await db.Robots.AsNoTracking().Where(x => x.Id == robotId).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
                : null;
            responses.Add(ToResponse(mission, robotName, task?.RobotId));
        }
        return Ok(responses);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        var task = await db.JobTasks.AsNoTracking().FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        var robot = task?.RobotId is Guid robotId
            ? await db.Robots.AsNoTracking().FirstOrDefaultAsync(x => x.Id == robotId, cancellationToken)
            : null;
        return Ok(ToResponse(mission, robot?.Name, task?.RobotId));
    }

    [HttpPost]
    public async Task<ActionResult<MissionResponse>> Create(MissionRequest request, CancellationToken cancellationToken)
    {
        if (request.WorkflowId is null || !Guid.TryParse(request.WorkflowId, out var workflowId))
            return BadRequest("A valid workflowId is required.");
        var workflow = await db.Workflows.FirstOrDefaultAsync(x => x.Id == workflowId, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        Guid? robotId = Guid.TryParse(request.RobotId, out var parsedRobotId) &&
            await db.Robots.AnyAsync(x => x.Id == parsedRobotId, cancellationToken) ? parsedRobotId : null;
        var mission = new Mission(request.Name ?? workflow.Name, request.Description ?? workflow.Description,
            sourceEndpointId: request.SourceEndpointId, destinationEndpointId: request.DestinationEndpointId,
            priority: ParsePriority(request.Priority));
        mission.Update(mission.Name, mission.Description, mission.Priority, workflowId,
            request.SourceEndpointId, request.DestinationEndpointId);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Jobs.Add(mission);
        await db.SaveChangesAsync(cancellationToken);

        var tasks = await db.WorkflowTasks.Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.SequenceNumber).ToListAsync(cancellationToken);
        var jobTask = new JobTask(mission.Id, robotId);
        db.JobTasks.Add(jobTask);
        await db.SaveChangesAsync(cancellationToken);
        var steps = await db.TaskSteps.Where(x => tasks.Select(t => t.Id).Contains(x.WorkflowTaskId)).ToListAsync(cancellationToken);
        var sequence = 1;
        foreach (var step in steps)
            db.JobSteps.Add(new JobStep(jobTask.Id, step.Id, sequence++));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var robotName = robotId is Guid id
            ? await db.Robots.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        return CreatedAtAction(nameof(GetById), new { id = mission.Id }, ToResponse(mission, robotName, robotId));
    }

    [HttpPost("preview")]
    public async Task<ActionResult<MissionPreviewResponse>> Preview(MissionPreviewRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.WorkflowId, out var workflowId)) return BadRequest("A valid workflowId is required.");
        var workflow = await db.Workflows.AsNoTracking().FirstOrDefaultAsync(x => x.Id == workflowId, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        var stepData = await db.WorkflowTasks.AsNoTracking().Where(x => x.WorkflowId == workflowId)
            .OrderBy(x => x.SequenceNumber)
            .SelectMany(task => db.TaskSteps.Where(step => step.WorkflowTaskId == task.Id)
                .Select(step => new { step.Id, task.SequenceNumber, step.StepType, task.Name, step.Configuration }))
            .ToListAsync(cancellationToken);
        var steps = stepData.Select(step => new WorkflowStepResponse(step.Id, step.SequenceNumber, step.StepType,
            step.Name, ParseConfiguration(step.Configuration))).ToList();
        Guid? robotId = Guid.TryParse(request.RobotId, out var parsedRobotId) ? parsedRobotId : null;
        Guid? mapId = Guid.TryParse(request.MapId, out var parsedMapId) ? parsedMapId : null;
        if (mapId.HasValue && !await db.Maps.AnyAsync(x => x.Id == mapId, cancellationToken))
            return NotFound("Map not found.");
        return Ok(new MissionPreviewResponse(workflowId, robotId, mapId,
            new[] { new Point(0, 0), new Point(1, 1) }, steps, 1, 30));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MissionResponse>> Update(Guid id, MissionUpdateRequest request, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        Guid? workflowId = null;
        if (!string.IsNullOrWhiteSpace(request.WorkflowId))
        {
            if (!Guid.TryParse(request.WorkflowId, out var parsedWorkflowId)) return BadRequest("Invalid workflowId.");
            if (!await db.Workflows.AnyAsync(x => x.Id == parsedWorkflowId, cancellationToken)) return NotFound("Workflow not found.");
            workflowId = parsedWorkflowId;
        }
        if (workflowId.HasValue && workflowId != mission.WorkflowId)
            return Conflict("A mission's workflow cannot be changed after creation.");
        mission.Update(request.Name ?? mission.Name, request.Description ?? mission.Description, ParsePriority(request.Priority),
            mission.WorkflowId, request.SourceEndpointId ?? mission.SourceEndpointId,
            request.DestinationEndpointId ?? mission.DestinationEndpointId);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(mission, null, null));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        db.Jobs.Remove(mission);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        try { mission.Cancel(); }
        catch (InvalidOperationException exception) { return Conflict(exception.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        try { mission.Start(); }
        catch (InvalidOperationException exception) { return Conflict(exception.Message); }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        mission.Complete();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static MissionResponse ToResponse(Mission mission, string? robotName, Guid? robotId) =>
        new(mission.Id, mission.WorkflowId, mission.Name, mission.Description,
            mission.Status == "Pending" ? "QUEUED" : mission.Status.ToUpperInvariant(), FormatPriority(mission.Priority),
            mission.CreatedAtUtc, mission.StartedAtUtc, mission.CompletedAtUtc, robotId, robotName);

    private static int ParsePriority(string? priority) => priority?.ToUpperInvariant() switch
    {
        "LOW" => 1,
        "MEDIUM" => 2,
        "HIGH" => 3,
        "CRITICAL" => 4,
        _ when int.TryParse(priority, out var value) => value,
        _ => 0
    };

    private static string FormatPriority(int priority) => priority switch
    {
        1 => "LOW",
        2 => "MEDIUM",
        3 => "HIGH",
        >= 4 => "CRITICAL",
        _ => "MEDIUM"
    };

    private static JsonElement? ParseConfiguration(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return null;
        try { return JsonDocument.Parse(configuration).RootElement.Clone(); }
        catch (JsonException) { return JsonSerializer.SerializeToElement(configuration); }
    }
}

public record MissionRequest(string? WorkflowId, string? RobotId, string? Priority, string? Name = null,
    string? Description = null, Guid? SourceEndpointId = null, Guid? DestinationEndpointId = null);
public record MissionUpdateRequest(string? Name, string? Description, string? Priority, string? WorkflowId = null,
    Guid? SourceEndpointId = null, Guid? DestinationEndpointId = null);
public record MissionPreviewRequest(string WorkflowId, string? RobotId = null, string? MapId = null);
public record MissionResponse(Guid Id, Guid? WorkflowId, string WorkflowName, string Description, string Status,
    string Priority, DateTime CreatedAt, DateTime? StartedAt, DateTime? CompletedAt, Guid? RobotId, string? RobotName);
public record MissionPreviewResponse(Guid WorkflowId, Guid? RobotId, Guid? MapId, IReadOnlyList<Point> Path,
    IReadOnlyList<WorkflowStepResponse> Steps, double EstimatedDistance, int EstimatedDuration);
public record Point(double X, double Y);
public record WorkflowStepResponse(Guid Id, int Sequence, string Type, string Name, JsonElement? Configuration);
