using System.Text.Json;
using LiBot.Api.Hubs;
using LiBot.Api.Services;
using LiBot.Application.Robot;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/missions")]
public class MissionController(
    LiBotDbContext db,
    WorkflowDocumentService workflowDocuments,
    ILiBotTcpClient tcpClient,
    RobotRuntimeService runtimes,
    IHubContext<RobotHub> hub,
    ILogger<MissionController> logger) : ControllerBase
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
            responses.Add(ToResponse(mission, robotName, task?.RobotId, task?.RuntimeId));
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
        return Ok(ToResponse(mission, robot?.Name, task?.RobotId, task?.RuntimeId));
    }

    [HttpPost]
    public async Task<ActionResult<MissionResponse>> Create(MissionRequest request, CancellationToken cancellationToken)
    {
        if (request.InitialContext is { } suppliedContext && suppliedContext.ValueKind != JsonValueKind.Object)
            return BadRequest(new { message = "initialContext must be a JSON object." });
        if (request.WorkflowId is null || !Guid.TryParse(request.WorkflowId, out var workflowId))
            return BadRequest("A valid workflowId is required.");
        var workflow = await db.Workflows.FirstOrDefaultAsync(x => x.Id == workflowId, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        if (!workflow.IsActive) return Conflict("The workflow is inactive.");
        var runtimeId = (request.RuntimeId ?? "real").Trim().ToLowerInvariant();
        if (runtimeId is not ("sim" or "real") || runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = "runtimeId must identify a configured 'sim' or 'real' runtime." });
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
        var jobTask = new JobTask(mission.Id, robotId, runtimeId);
        db.JobTasks.Add(jobTask);
        await db.SaveChangesAsync(cancellationToken);
        var steps = await db.TaskSteps.Where(x => tasks.Select(t => t.Id).Contains(x.WorkflowTaskId)).ToListAsync(cancellationToken);
        var sequence = 1;
        foreach (var step in steps)
            db.JobSteps.Add(new JobStep(jobTask.Id, step.Id, sequence++));
        if (request.InitialContext is { ValueKind: JsonValueKind.Object } initialContext)
            db.JobEvents.Add(new JobEvent(mission.Id, "WORKFLOW_INITIAL_CONTEXT", initialContext.GetRawText()));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var robotName = robotId is Guid id
            ? await db.Robots.Where(x => x.Id == id).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        return CreatedAtAction(nameof(GetById), new { id = mission.Id }, ToResponse(mission, robotName, robotId, runtimeId));
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
        var task = await db.JobTasks.FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        if (task?.ExecutionId is string executionId && mission.Status == "Running")
        {
            try
            {
                using var response = await tcpClient.SendAsync(task.RuntimeId, new
                {
                    command = "cancel_workflow",
                    execution_id = executionId
                }, cancellationToken);
                if (!HasStatus(response.RootElement, "accepted"))
                    return Conflict(new { message = ReadMessage(response.RootElement) ?? "Robot did not cancel the workflow." });
            }
            catch (LiBotTcpException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
        try { mission.Cancel(); }
        catch (InvalidOperationException exception) { return Conflict(exception.Message); }
        if (task is not null)
            task.UpdateExecutionState(JsonSerializer.Serialize(new { missionId = id, executionId = task.ExecutionId,
                runtimeId = task.RuntimeId, status = "CANCELLED", updatedAt = DateTime.UtcNow }));
        await db.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = "CANCELLED" }, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] MissionStartRequest? request, CancellationToken cancellationToken)
    {
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        if (mission.WorkflowId is not Guid workflowId) return Conflict("Mission has no workflow.");
        var jobTask = await db.JobTasks.FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        if (jobTask is null) return Conflict("Mission has no robot task.");
        if (jobTask.ExecutionId is not null) return Conflict("Mission has already been dispatched.");

        var preflightError = runtimes.ValidateBookGuidanceStart(jobTask.RuntimeId, request?.ArmLeaseId);
        if (preflightError is not null) return Conflict(new { message = preflightError });

        RobotWorkflowDocument document;
        try
        {
            document = await workflowDocuments.BuildRobotDocumentAsync(workflowId, jobTask.RuntimeId, cancellationToken)
                ?? throw new InvalidWorkflowDefinitionException("Workflow was not found.");
        }
        catch (InvalidWorkflowDefinitionException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        var executionId = Guid.NewGuid().ToString("N");
        var initialContext = await ReadInitialContextAsync(id, cancellationToken);
        initialContext["mission_id"] = id.ToString();
        initialContext["task_id"] = id.ToString();
        initialContext["runtime_id"] = jobTask.RuntimeId;
        initialContext["backend_base_url"] = $"{Request.Scheme}://{Request.Host.Value}";

        JsonElement preflightResponse;
        try
        {
            using var response = await tcpClient.SendAsync(jobTask.RuntimeId, new
            {
                command = "preflight_workflow",
                runtime_id = jobTask.RuntimeId,
                require_hardware = jobTask.RuntimeId == "real"
            }, cancellationToken);
            preflightResponse = response.RootElement.Clone();
        }
        catch (LiBotTcpException exception)
        {
            logger.LogWarning(exception, "Workflow preflight failed for mission {MissionId} on {RuntimeId}.", id, jobTask.RuntimeId);
            return Conflict(new { message = exception.Message });
        }
        if (!HasStatus(preflightResponse, "ok"))
            return Conflict(new { message = ReadMessage(preflightResponse) ?? "Robot preflight failed." });

        try
        {
            mission.Start();
            jobTask.Start();
            jobTask.SetExecutionId(executionId);
            jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
            {
                missionId = id,
                executionId,
                runtimeId = jobTask.RuntimeId,
                status = "ACCEPTED",
                currentStep = (string?)null,
                currentStepType = (string?)null,
                expectedEventType = (string?)null,
                allowedValues = Array.Empty<string>(),
                updatedAt = DateTime.UtcNow
            }));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }

        try
        {
            using var response = await tcpClient.SendAsync(jobTask.RuntimeId, new
            {
                command = "start_workflow",
                workflow_version = document.Version,
                workflow_id = document.WorkflowId,
                mission_id = id.ToString(),
                task_id = id.ToString(),
                execution_id = executionId,
                runtime_id = jobTask.RuntimeId,
                initial_context = initialContext,
                steps = document.Steps
            }, cancellationToken);
            var payload = response.RootElement.Clone();
            if (!HasStatus(payload, "accepted"))
            {
                mission.Fail();
                jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
                {
                    missionId = id, executionId, runtimeId = jobTask.RuntimeId, status = "FAILED",
                    error = ReadMessage(payload), updatedAt = DateTime.UtcNow
                }));
                await db.SaveChangesAsync(cancellationToken);
                return Conflict(new { message = ReadMessage(payload) ?? "Robot rejected the workflow." });
            }
            return Accepted(new { missionId = id, executionId, runtimeId = jobTask.RuntimeId, status = "ACCEPTED" });
        }
        catch (LiBotTcpException exception)
        {
            mission.Fail();
            jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
            {
                missionId = id, executionId, runtimeId = jobTask.RuntimeId, status = "FAILED",
                error = exception.Message, updatedAt = DateTime.UtcNow
            }));
            await db.SaveChangesAsync(cancellationToken);
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{id:guid}/execution")]
    public async Task<IActionResult> GetExecution(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.AsNoTracking().FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        if (task is null) return NotFound();
        if (string.IsNullOrWhiteSpace(task.ExecutionState))
            return Ok(new { missionId = id, runtimeId = task.RuntimeId, executionId = task.ExecutionId, status = "QUEUED" });
        try
        {
            using var document = JsonDocument.Parse(task.ExecutionState);
            return Ok(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return Ok(new { missionId = id, runtimeId = task.RuntimeId, executionId = task.ExecutionId, status = "UNKNOWN" });
        }
    }

    [HttpPost("{id:guid}/execution/events")]
    public async Task<IActionResult> SubmitExecutionEvent(
        Guid id,
        MissionExecutionEventRequest request,
        CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        if (task is null) return NotFound();
        if (string.IsNullOrWhiteSpace(task.ExecutionId)) return Conflict("Mission has not started.");
        if (string.IsNullOrWhiteSpace(request.EventType)) return BadRequest(new { message = "eventType is required." });
        try
        {
            using var response = await tcpClient.SendAsync(task.RuntimeId, new
            {
                command = "submit_event",
                execution_id = task.ExecutionId,
                event_type = request.EventType.Trim().ToUpperInvariant(),
                data = request.Data
            }, cancellationToken);
            var payload = response.RootElement.Clone();
            return HasStatus(payload, "accepted") ? Accepted(payload) : Conflict(payload);
        }
        catch (LiBotTcpException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpPost("{id:guid}/execution/events/state")]
    public async Task<IActionResult> UpdateExecutionState(
        Guid id,
        MissionExecutionStateRequest request,
        CancellationToken cancellationToken)
    {
        var supportedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ACCEPTED", "RUNNING", "WAITING", "STEP_COMPLETED", "STEP_FAILED", "SUCCEEDED", "COMPLETED", "FAILED", "CANCELLED"
        };
        if (string.IsNullOrWhiteSpace(request.Status) || !supportedStatuses.Contains(request.Status))
            return BadRequest(new { message = "Unsupported workflow execution status." });
        var task = await db.JobTasks.FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        if (task is null)
        {
            var legacyMission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (legacyMission is null) return NotFound();
            if (legacyMission.Status == "Pending") legacyMission.Start();
            task = new JobTask(id, null, request.RuntimeId ?? "real");
            task.Start();
            if (!string.IsNullOrWhiteSpace(request.ExecutionId)) task.SetExecutionId(request.ExecutionId);
            db.JobTasks.Add(task);
        }
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        if (mission.Status is "Completed" or "Failed" or "Cancelled")
            return Accepted(new { missionId = id, executionId = task.ExecutionId, runtimeId = task.RuntimeId, status = mission.Status.ToUpperInvariant() });
        if (!string.IsNullOrWhiteSpace(request.ExecutionId)
            && !string.Equals(task.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
            return Conflict(new { message = "Execution id does not match the active mission." });
        if (!string.IsNullOrWhiteSpace(request.RuntimeId)
            && !string.Equals(task.RuntimeId, request.RuntimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime id does not match the assigned mission runtime." });

        var state = new
        {
            missionId = id,
            executionId = task.ExecutionId,
            runtimeId = task.RuntimeId,
            status = request.Status,
            currentStep = request.StepId,
            currentStepName = request.StepName,
            currentStepType = request.StepType,
            expectedEventType = request.ExpectedEventType,
            allowedValues = request.AllowedValues ?? [],
            result = request.Result,
            error = request.Error,
            updatedAt = DateTime.UtcNow
        };
        task.UpdateExecutionState(JsonSerializer.Serialize(state));
        db.JobEvents.Add(new JobEvent(id, "WORKFLOW_STEP", JsonSerializer.Serialize(state)));
        await db.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("MissionStepChanged", state, cancellationToken);
        return Accepted(state);
    }

    [HttpPost("{id:guid}/execution/completed")]
    public async Task<IActionResult> CompleteExecution(
        Guid id,
        MissionExecutionCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "status must be SUCCEEDED, COMPLETED, FAILED, or CANCELLED." });
        var normalizedStatus = request.Status.Trim().ToUpperInvariant();
        if (normalizedStatus is not ("SUCCEEDED" or "COMPLETED" or "FAILED" or "CANCELLED"))
            return BadRequest(new { message = "status must be SUCCEEDED, COMPLETED, FAILED, or CANCELLED." });
        var mission = await db.Jobs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (mission is null) return NotFound();
        var task = await db.JobTasks.FirstOrDefaultAsync(x => x.JobId == id, cancellationToken);
        if (task is null)
        {
            var legacySucceeded = normalizedStatus is "SUCCEEDED" or "COMPLETED";
            if (mission.Status is not ("Completed" or "Failed" or "Cancelled"))
            {
                if (normalizedStatus == "CANCELLED") mission.Cancel();
                else if (legacySucceeded) mission.Complete();
                else mission.Fail();
            }
            await db.SaveChangesAsync(cancellationToken);
            await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = normalizedStatus }, cancellationToken);
            return Accepted(new { missionId = id, status = normalizedStatus });
        }
        if (!string.IsNullOrWhiteSpace(request.ExecutionId)
            && !string.Equals(task.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
            return Conflict(new { message = "Execution id does not match the active mission." });

        var cancelled = normalizedStatus == "CANCELLED";
        var succeeded = normalizedStatus is "SUCCEEDED" or "COMPLETED";
        if (mission.Status is not ("Completed" or "Failed" or "Cancelled"))
        {
            if (cancelled) mission.Cancel();
            else if (succeeded) mission.Complete();
            else mission.Fail();
        }
        try { task.Complete(); } catch (InvalidOperationException) { }
        var state = new
        {
            missionId = id,
            executionId = task.ExecutionId,
            runtimeId = task.RuntimeId,
            status = cancelled ? "CANCELLED" : succeeded ? "SUCCEEDED" : "FAILED",
            currentStep = (string?)null,
            currentStepType = (string?)null,
            error = request.Message,
            updatedAt = DateTime.UtcNow
        };
        task.UpdateExecutionState(JsonSerializer.Serialize(state));
        await db.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = state.status }, cancellationToken);
        await hub.Clients.All.SendAsync("MissionStepChanged", state, cancellationToken);
        return Accepted(state);
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

    private static MissionResponse ToResponse(Mission mission, string? robotName, Guid? robotId, string? runtimeId = null) =>
        new(mission.Id, mission.WorkflowId, mission.Name, mission.Description,
            mission.Status == "Pending" ? "QUEUED" : mission.Status.ToUpperInvariant(), FormatPriority(mission.Priority),
            mission.CreatedAtUtc, mission.StartedAtUtc, mission.CompletedAtUtc, robotId, robotName, runtimeId);

    private async Task<Dictionary<string, object?>> ReadInitialContextAsync(Guid missionId, CancellationToken cancellationToken)
    {
        var raw = await db.JobEvents.AsNoTracking()
            .Where(item => item.JobId == missionId && item.EventType == "WORKFLOW_INITIAL_CONTEXT")
            .OrderByDescending(item => item.EventTimeUtc)
            .Select(item => item.Data)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw)) return new Dictionary<string, object?>();
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new Dictionary<string, object?>();
            return document.RootElement.EnumerateObject().ToDictionary(item => item.Name, item => (object?)item.Value.Clone());
        }
        catch (JsonException) { return new Dictionary<string, object?>(); }
    }

    private static bool HasStatus(JsonElement payload, string expected) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty("status", out var status)
        && status.ValueKind == JsonValueKind.String
        && string.Equals(status.GetString(), expected, StringComparison.OrdinalIgnoreCase);

    private static string? ReadMessage(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String ? message.GetString() : null;

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
    string? Description = null, Guid? SourceEndpointId = null, Guid? DestinationEndpointId = null,
    string? RuntimeId = null, JsonElement? InitialContext = null);
public record MissionUpdateRequest(string? Name, string? Description, string? Priority, string? WorkflowId = null,
    Guid? SourceEndpointId = null, Guid? DestinationEndpointId = null);
public record MissionPreviewRequest(string WorkflowId, string? RobotId = null, string? MapId = null);
public record MissionResponse(Guid Id, Guid? WorkflowId, string WorkflowName, string Description, string Status,
    string Priority, DateTime CreatedAt, DateTime? StartedAt, DateTime? CompletedAt, Guid? RobotId, string? RobotName,
    string? RuntimeId = null);
public record MissionPreviewResponse(Guid WorkflowId, Guid? RobotId, Guid? MapId, IReadOnlyList<Point> Path,
    IReadOnlyList<WorkflowStepResponse> Steps, double EstimatedDistance, int EstimatedDuration);
public record Point(double X, double Y);
public record WorkflowStepResponse(Guid Id, int Sequence, string Type, string Name, JsonElement? Configuration);
public record MissionStartRequest(string? ArmLeaseId = null);
public record MissionExecutionEventRequest(string EventType, JsonElement? Data = null);
public record MissionExecutionStateRequest(string? ExecutionId, string? RuntimeId, string Status,
    string? StepId = null, string? StepName = null, string? StepType = null,
    string? ExpectedEventType = null, IReadOnlyList<string>? AllowedValues = null,
    JsonElement? Result = null, string? Error = null);
public record MissionExecutionCompletionRequest(string Status, string? ExecutionId = null, string? Message = null);
