using System.Text.Json;
using LiBot.Api.Hubs;
using LiBot.Application.Robot;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Tạo và quản lý mission; khởi chạy workflow robot và cung cấp trạng thái thực thi.</summary>
/// <remarks>Mission trên runtime SIM giữ trạng thái thực thi tạm thời; mission trên runtime REAL được lưu bền vững.</remarks>
[ApiController]
[Route("api/missions")]
public class MissionController(
    IMissionDataService missionData,
    IWorkflowDocumentService workflowDocuments,
    ILiBotTcpClient tcpClient,
    IRobotRuntimeService runtimes,
    IHubContext<RobotHub> hub,
    ILogger<MissionController> logger) : ControllerBase
{
    /// <summary>Lấy danh sách mission cùng robot, runtime và trạng thái hiện tại.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MissionResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var missions = await missionData.GetAllMissionsAsync(cancellationToken);
        var responses = new List<MissionResponse>();
        foreach (var mission in missions)
        {
            var task = await missionData.GetTaskAsync(mission.Id, tracking: false, cancellationToken);
            var robotName = await missionData.GetRobotNameAsync(mission.RobotId ?? Guid.Empty, cancellationToken);
            responses.Add(ToResponse(mission, robotName, mission.RobotId, task?.RuntimeId,
                GetTransientSimulationStatus(mission.Id, task?.RuntimeId)));
        }
        return Ok(responses);
    }

    /// <summary>Lấy thông tin một mission theo ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MissionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var mission = await missionData.GetMissionAsync(id, tracking: false, cancellationToken);
        if (mission is null) return NotFound();
        var task = await missionData.GetTaskAsync(id, tracking: false, cancellationToken);
        var robot = await missionData.GetRobotNameAsync(mission.RobotId ?? Guid.Empty, cancellationToken);
        return Ok(ToResponse(mission, robot, mission.RobotId, task?.RuntimeId,
            GetTransientSimulationStatus(mission.Id, task?.RuntimeId)));
    }

    /// <summary>Tạo mission gắn với workflow và runtime được chọn.</summary>
    [HttpPost]
    public async Task<ActionResult<MissionResponse>> Create(MissionRequest request, CancellationToken cancellationToken)
    {
        if (request.InitialContext is { } suppliedContext && suppliedContext.ValueKind != JsonValueKind.Object)
            return BadRequest(new { message = "initialContext must be a JSON object." });
        if (request.WorkflowId is null || !Guid.TryParse(request.WorkflowId, out var workflowId))
            return BadRequest("A valid workflowId is required.");
        var workflow = await missionData.GetWorkflowAsync(workflowId, tracking: false, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        if (!workflow.IsActive) return Conflict("The workflow is inactive.");
        var runtimeId = (request.RuntimeId ?? "real").Trim().ToLowerInvariant();
        if (runtimeId is not ("sim" or "real") || runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = "runtimeId must identify a configured 'sim' or 'real' runtime." });
        if (!Guid.TryParse(request.RobotId, out var robotId))
            return BadRequest("A valid robotId is required.");
        if (!await missionData.RobotExistsAsync(robotId, cancellationToken))
            return NotFound("Robot not found.");
        var mission = new Mission(request.Name ?? workflow.Name, request.Description ?? workflow.Description,
            priority: ParsePriority(request.Priority));
        mission.AssignRobot(robotId);
        mission.Update(mission.Name, mission.Description, mission.Priority, workflowId);
        var initialContextJson = request.InitialContext is { ValueKind: JsonValueKind.Object } initialContext
            ? initialContext.GetRawText()
            : null;
        await missionData.CreateMissionAsync(mission, workflowId, runtimeId, initialContextJson, cancellationToken);

        var robotName = await missionData.GetRobotNameAsync(robotId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = mission.Id }, ToResponse(mission, robotName, robotId, runtimeId));
    }

    /// <summary>Xem trước task/step của workflow trước khi tạo hoặc chạy mission.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<MissionPreviewResponse>> Preview(MissionPreviewRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.WorkflowId, out var workflowId)) return BadRequest("A valid workflowId is required.");
        var workflow = await missionData.GetWorkflowAsync(workflowId, tracking: false, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        var steps = await missionData.GetWorkflowStepsAsync(workflowId, cancellationToken);
        Guid? robotId = Guid.TryParse(request.RobotId, out var parsedRobotId) ? parsedRobotId : null;
        Guid? mapId = Guid.TryParse(request.MapId, out var parsedMapId) ? parsedMapId : null;
        if (mapId.HasValue && !await missionData.MapExistsAsync(mapId.Value, cancellationToken))
            return NotFound("Map not found.");
        return Ok(new MissionPreviewResponse(workflowId, robotId, mapId,
            new[] { new Point(0, 0), new Point(1, 1) }, steps, 1, 30));
    }

    /// <summary>Cập nhật mô tả, ưu tiên, điểm bắt đầu hoặc điểm đến của mission.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MissionResponse>> Update(Guid id, MissionUpdateRequest request, CancellationToken cancellationToken)
    {
        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        Guid? workflowId = null;
        if (!string.IsNullOrWhiteSpace(request.WorkflowId))
        {
            if (!Guid.TryParse(request.WorkflowId, out var parsedWorkflowId)) return BadRequest("Invalid workflowId.");
            if (!await missionData.WorkflowExistsAsync(parsedWorkflowId, cancellationToken)) return NotFound("Workflow not found.");
            workflowId = parsedWorkflowId;
        }
        if (workflowId.HasValue && workflowId != mission.WorkflowId)
            return Conflict("A mission's workflow cannot be changed after creation.");
        mission.Update(request.Name ?? mission.Name, request.Description ?? mission.Description,
            ParsePriority(request.Priority), mission.WorkflowId, mission.SourceEndpointId, mission.DestinationEndpointId);
        await missionData.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(mission, null, null));
    }

    /// <summary>Xóa mission theo ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        await missionData.DeleteMissionAsync(mission, cancellationToken);
        return NoContent();
    }

    /// <summary>Hủy mission đang chạy và gửi lệnh hủy đến runtime robot.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        var task = await missionData.GetTaskAsync(id, tracking: true, cancellationToken);
        if ((task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            || SimulationExecutionStore.TryGet(id, out _))
        {
            if (!SimulationExecutionStore.TryGetExecution(id, out var simulationExecutionId, out var simulationRuntimeId))
                return Conflict("Simulation mission has not been started.");
            try
            {
                using var response = await tcpClient.SendAsync(simulationRuntimeId, new
                {
                    command = "cancel_workflow",
                    execution_id = simulationExecutionId
                }, cancellationToken);
                if (!HasStatus(response.RootElement, "accepted"))
                    return Conflict(new { message = ReadMessage(response.RootElement) ?? "Robot did not cancel the workflow." });
            }
            catch (LiBotTcpException exception)
            {
                return Conflict(new { message = exception.Message });
            }
            var cancelledState = new
            {
                missionId = id,
                executionId = simulationExecutionId,
                runtimeId = simulationRuntimeId,
                status = "CANCELLED",
                updatedAt = DateTime.UtcNow
            };
            if (!SimulationExecutionStore.TryUpdate(id, simulationExecutionId, cancelledState))
                return Conflict("Simulation mission has already finished.");
            await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = "CANCELLED" }, cancellationToken);
            await hub.Clients.All.SendAsync("MissionStepChanged", cancelledState, cancellationToken);
            return NoContent();
        }
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
        await missionData.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = "CANCELLED" }, cancellationToken);
        return NoContent();
    }

    /// <summary>Chạy workflow của mission trên runtime SIM hoặc REAL.</summary>
    /// <remarks>Với SIM, trạng thái được giữ trong bộ nhớ và không cập nhật trạng thái mission trong database.</remarks>
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] MissionStartRequest? request, CancellationToken cancellationToken)
    {
        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        if (mission.WorkflowId is not Guid workflowId) return Conflict("Mission has no workflow.");
        var jobTask = await missionData.GetTaskAsync(id, tracking: true, cancellationToken);
        var isRetry = mission.Status is "Failed" or "Cancelled";
        var requestedRuntimeId = string.IsNullOrWhiteSpace(request?.RuntimeId)
            ? null
            : request.RuntimeId.Trim().ToLowerInvariant();
        if (jobTask is not null && requestedRuntimeId is not null
            && !string.Equals(jobTask.RuntimeId, requestedRuntimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime id does not match the mission's assigned runtime." });
        var runtimeId = jobTask?.RuntimeId ?? requestedRuntimeId ?? "sim";
        var isSimulation = string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase);

        // A simulation run is transient: the mission row remains QUEUED in the database,
        // so its run eligibility and prior run's ExecutionId must not gate a new click.
        if (!isSimulation && (mission.Status == "Running" || mission.Status == "Completed"))
            return Conflict("Only queued, failed, or cancelled missions can start.");
        if (!isSimulation && jobTask?.ExecutionId is not null && !isRetry)
            return Conflict("Mission has already been dispatched.");
        if (runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = $"Robot runtime '{runtimeId}' is not configured." });

        var preflightError = runtimes.ValidateGuidanceStart(runtimeId, request?.ArmLeaseId);
        if (preflightError is not null) return Conflict(new { message = preflightError });

        RobotWorkflowDocument document;
        try
        {
            document = await workflowDocuments.BuildRobotDocumentAsync(workflowId, runtimeId, cancellationToken)
                ?? throw new InvalidWorkflowDefinitionException("Workflow was not found.");
        }
        catch (InvalidWorkflowDefinitionException exception)
        {
            return BadRequest(new { message = exception.Message });
        }

        if (jobTask is null && !isSimulation)
            jobTask = await CreateMissingJobTaskAsync(id, workflowId, runtimeId, cancellationToken);

        var executionId = Guid.NewGuid().ToString("N");
        var initialContext = await ReadInitialContextAsync(id, cancellationToken);
        RestoreLegacyGuidanceQuery(mission, initialContext);
        initialContext["mission_id"] = id.ToString();
        initialContext["job_id"] = id.ToString();
        initialContext["task_id"] = id.ToString();
        initialContext["runtime_id"] = runtimeId;
        initialContext["execution_id"] = executionId;
        if (runtimes.Get(runtimeId)?.Telemetry?.Pose is { } currentPose)
            initialContext["current_pose"] = new { x = currentPose.X, y = currentPose.Y, theta = currentPose.Yaw };
        initialContext["backend_base_url"] = $"{Request.Scheme}://{Request.Host.Value}";

        JsonElement preflightResponse;
        try
        {
            using var response = await tcpClient.SendAsync(runtimeId, new
            {
                command = "preflight_workflow",
                runtime_id = runtimeId,
                require_hardware = !isSimulation,
                require_robot_ui = RequiresRobotUi(document.Steps)
            }, cancellationToken);
            preflightResponse = response.RootElement.Clone();
        }
        catch (LiBotTcpException exception)
        {
            logger.LogWarning(exception, "Workflow preflight failed for mission {MissionId} on {RuntimeId}.", id, runtimeId);
            return Conflict(new { message = exception.Message });
        }
        if (!HasStatus(preflightResponse, "ok"))
            return Conflict(new { message = ReadMessage(preflightResponse) ?? "Robot preflight failed." });

        var acceptedState = new
        {
            missionId = id,
            executionId,
            runtimeId,
            status = "ACCEPTED",
            currentStep = (string?)null,
            currentStepType = (string?)null,
            expectedEventType = (string?)null,
            allowedValues = Array.Empty<string>(),
            updatedAt = DateTime.UtcNow
        };

        if (isSimulation)
        {
            if (!SimulationExecutionStore.TryStart(id, executionId, runtimeId, acceptedState))
                return Conflict(new { message = "This simulation mission is already running." });
        }
        else
        {
            try
            {
                if (isRetry)
                {
                    mission.Retry();
                    jobTask!.PrepareForRetry();
                }
                mission.Start();
                jobTask!.Start();
                jobTask.SetExecutionId(executionId);
                jobTask.UpdateExecutionState(JsonSerializer.Serialize(acceptedState));
                await missionData.SaveChangesAsync(cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(exception.Message);
            }
        }

        try
        {
            using var response = await tcpClient.SendAsync(runtimeId, new
            {
                command = "start_workflow",
                workflow_version = document.Version,
                workflow_id = document.WorkflowId,
                mission_id = id.ToString(),
                task_id = id.ToString(),
                execution_id = executionId,
                runtime_id = runtimeId,
                initial_context = initialContext,
                steps = document.Steps
            }, cancellationToken);
            var payload = response.RootElement.Clone();
            if (!HasStatus(payload, "accepted"))
            {
                var failedState = new
                {
                    missionId = id, executionId, runtimeId, status = "FAILED",
                    error = ReadMessage(payload), updatedAt = DateTime.UtcNow
                };
                if (isSimulation) SimulationExecutionStore.TryUpdate(id, executionId, failedState);
                else
                {
                    mission.Fail();
                    jobTask!.UpdateExecutionState(JsonSerializer.Serialize(failedState));
                    await missionData.SaveChangesAsync(cancellationToken);
                }
                return Conflict(new { message = ReadMessage(payload) ?? "Robot rejected the workflow." });
            }
            return Accepted(new { missionId = id, executionId, runtimeId, status = "ACCEPTED" });
        }
        catch (LiBotTcpException exception)
        {
            var failedState = new
            {
                missionId = id, executionId, runtimeId, status = "FAILED",
                error = exception.Message, updatedAt = DateTime.UtcNow
            };
            if (isSimulation) SimulationExecutionStore.TryUpdate(id, executionId, failedState);
            else
            {
                mission.Fail();
                jobTask!.UpdateExecutionState(JsonSerializer.Serialize(failedState));
                await missionData.SaveChangesAsync(cancellationToken);
            }
            return Conflict(new { message = exception.Message });
        }
    }

    /// <summary>Lấy trạng thái lần chạy hiện tại, ví dụ QUEUED, RUNNING, WAITING hoặc SUCCEEDED.</summary>
    [HttpGet("{id:guid}/execution")]
    public async Task<IActionResult> GetExecution(Guid id, CancellationToken cancellationToken)
    {
        var task = await missionData.GetTaskAsync(id, tracking: false, cancellationToken);
        if (SimulationExecutionStore.TryGet(id, out var simulation)) return Ok(simulation.CurrentState);
        if (task is null)
        {
            var exists = await missionData.MissionExistsAsync(id, cancellationToken);
            return exists
                ? Ok(new { missionId = id, runtimeId = "sim", executionId = (string?)null, status = "QUEUED" })
                : NotFound();
        }
        if (string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Ok(new { missionId = id, runtimeId = task.RuntimeId, executionId = (string?)null, status = "QUEUED" });
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

    /// <summary>Lấy lịch sử cập nhật step của lần chạy mission.</summary>
    [HttpGet("{id:guid}/execution/events")]
    public async Task<IActionResult> GetExecutionEvents(Guid id, CancellationToken cancellationToken)
    {
        if (SimulationExecutionStore.TryGet(id, out var simulation)) return Ok(simulation.Events);
        var task = await missionData.GetTaskAsync(id, tracking: false, cancellationToken);
        if (task is null || string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Ok(Array.Empty<JsonElement>());
        var executionId = task.ExecutionId;
        if (string.IsNullOrWhiteSpace(executionId)) return Ok(Array.Empty<JsonElement>());
        var stateDocuments = await missionData.GetWorkflowStepEventDataAsync(id, cancellationToken);
        var states = new List<JsonElement>(stateDocuments.Count);
        foreach (var data in stateDocuments)
        {
            if (string.IsNullOrWhiteSpace(data)) continue;
            try
            {
                using var document = JsonDocument.Parse(data);
                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("executionId", out var eventExecutionId)
                    && eventExecutionId.ValueKind == JsonValueKind.String
                    && string.Equals(eventExecutionId.GetString(), executionId, StringComparison.Ordinal))
                    states.Add(document.RootElement.Clone());
            }
            catch (JsonException) { }
        }

        return Ok(states);
    }

    /// <summary>Gửi sự kiện hoặc câu trả lời của người dùng đến workflow robot đang chạy.</summary>
    [HttpPost("{id:guid}/execution/events")]
    public async Task<IActionResult> SubmitExecutionEvent(
        Guid id,
        MissionExecutionEventRequest request,
        CancellationToken cancellationToken)
    {
        var task = await missionData.GetTaskAsync(id, tracking: true, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.EventType)) return BadRequest(new { message = "eventType is required." });
        string executionId;
        string runtimeId;
        if (SimulationExecutionStore.TryGetExecution(id, out var simulationExecutionId, out var simulationRuntimeId))
        {
            executionId = simulationExecutionId;
            runtimeId = simulationRuntimeId;
        }
        else
        {
            if (task is null) return NotFound();
            if (string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
                return Conflict("Simulation mission has not started in this API session.");
            if (string.IsNullOrWhiteSpace(task.ExecutionId)) return Conflict("Mission has not started.");
            executionId = task.ExecutionId;
            runtimeId = task.RuntimeId;
        }
        try
        {
            using var response = await tcpClient.SendAsync(runtimeId, new
            {
                command = "submit_event",
                execution_id = executionId,
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

    /// <summary>Nhận callback trạng thái step từ Pi và phát cập nhật realtime qua SignalR.</summary>
    [HttpPost("{id:guid}/execution/events/state")]
    public async Task<IActionResult> UpdateExecutionState(
        Guid id,
        MissionExecutionStateRequest request,
        CancellationToken cancellationToken)
    {
        var supportedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "QUEUED", "ACCEPTED", "RUNNING", "WAITING", "STEP_COMPLETED", "STEP_FAILED", "SUCCEEDED", "COMPLETED", "FAILED", "CANCELLED"
        };
        if (string.IsNullOrWhiteSpace(request.Status) || !supportedStatuses.Contains(request.Status))
            return BadRequest(new { message = "Unsupported workflow execution status." });

        if (SimulationExecutionStore.TryGet(id, out var simulation))
        {
            if (!string.IsNullOrWhiteSpace(request.RuntimeId)
                && !string.Equals(simulation.RuntimeId, request.RuntimeId, StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = "Runtime id does not match the active simulation." });
            var simulationState = new
            {
                missionId = id,
                executionId = simulation.ExecutionId,
                runtimeId = simulation.RuntimeId,
                status = request.Status.ToUpperInvariant(),
                currentStep = request.StepId,
                currentStepName = request.StepName,
                currentStepType = request.StepType,
                expectedEventType = request.ExpectedEventType,
                allowedValues = request.AllowedValues ?? [],
                expectedBookId = request.ExpectedBookId,
                result = request.Result,
                error = request.Error,
                updatedAt = DateTime.UtcNow
            };
            if (!SimulationExecutionStore.TryUpdate(id, request.ExecutionId, simulationState, recordEvent: true))
                return Conflict(new { message = "Execution id does not match the active simulation or the run has finished." });
            await hub.Clients.All.SendAsync("MissionStepChanged", simulationState, cancellationToken);
            return Accepted(simulationState);
        }

        var task = await missionData.GetTaskAsync(id, tracking: true, cancellationToken);
        if (task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase)
            || task is null && string.Equals(request.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "No active in-memory simulation exists for this mission." });
        if (task is null)
        {
            var legacyMission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
            if (legacyMission is null) return NotFound();
            if (legacyMission.Status == "Pending") legacyMission.Start();
            if (legacyMission.WorkflowId is not Guid legacyWorkflowId) return Conflict("Mission has no workflow.");
            task = await missionData.CreateMissingJobTaskAsync(
                id, legacyWorkflowId, request.RuntimeId ?? "real", cancellationToken);
            task.Start();
            if (!string.IsNullOrWhiteSpace(request.ExecutionId)) task.SetExecutionId(request.ExecutionId);
            await missionData.SaveChangesAsync(cancellationToken);
        }
        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        if (mission.Status is "Completed" or "Failed" or "Cancelled")
            return Accepted(new { missionId = id, executionId = task.ExecutionId, runtimeId = task.RuntimeId, status = mission.Status.ToUpperInvariant() });
        if (!string.IsNullOrWhiteSpace(request.ExecutionId)
            && !string.Equals(task.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
            return Conflict(new { message = "Execution id does not match the active mission." });
        if (!string.IsNullOrWhiteSpace(request.RuntimeId)
            && !string.Equals(task.RuntimeId, request.RuntimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime id does not match the assigned mission runtime." });

        if (string.Equals(request.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
        {
            if (mission.Status == "Pending") mission.Start();
            if (!task.StartedAtUtc.HasValue) task.Start();
        }

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
            expectedBookId = request.ExpectedBookId,
            result = request.Result,
            error = request.Error,
            updatedAt = DateTime.UtcNow
        };
        task.UpdateExecutionState(JsonSerializer.Serialize(state));
        await missionData.AddWorkflowStepEventAsync(id, JsonSerializer.Serialize(state), cancellationToken);
        await PublishLibrarianWorkflowNotificationAsync(id, task.RuntimeId, request, cancellationToken);
        await hub.Clients.All.SendAsync("MissionStepChanged", state, cancellationToken);
        return Accepted(state);
    }

    /// <summary>Nhận callback kết thúc workflow và cập nhật trạng thái mission.</summary>
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

        if (SimulationExecutionStore.TryGet(id, out var simulation))
        {
            if (!string.IsNullOrWhiteSpace(request.ExecutionId)
                && !string.Equals(simulation.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
                return Conflict(new { message = "Execution id does not match the active simulation." });
            var simulationStatus = normalizedStatus is "SUCCEEDED" or "COMPLETED" ? "SUCCEEDED" : normalizedStatus;
            var simulationState = new
            {
                missionId = id,
                executionId = simulation.ExecutionId,
                runtimeId = simulation.RuntimeId,
                status = simulationStatus,
                currentStep = (string?)null,
                currentStepType = (string?)null,
                error = request.Message,
                updatedAt = DateTime.UtcNow
            };
            if (!SimulationExecutionStore.TryUpdate(id, simulation.ExecutionId, simulationState))
                return Accepted(simulation.CurrentState);
            await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = simulationStatus }, cancellationToken);
            await hub.Clients.All.SendAsync("MissionStepChanged", simulationState, cancellationToken);
            return Accepted(simulationState);
        }

        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        var task = await missionData.GetTaskAsync(id, tracking: true, cancellationToken);
        if ((task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            || task is null)
            return Conflict(new { message = "No active in-memory simulation exists for this mission." });
        if (task is null)
        {
            var legacySucceeded = normalizedStatus is "SUCCEEDED" or "COMPLETED";
            if (mission.Status is not ("Completed" or "Failed" or "Cancelled"))
            {
                if (normalizedStatus == "CANCELLED") mission.Cancel();
                else if (legacySucceeded) mission.Complete();
                else mission.Fail();
            }
            await missionData.SaveChangesAsync(cancellationToken);
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
        await missionData.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("MissionStatusChanged", new { missionId = id, status = state.status }, cancellationToken);
        await hub.Clients.All.SendAsync("MissionStepChanged", state, cancellationToken);
        return Accepted(state);
    }

    /// <summary>Đánh dấu mission hoàn thành thủ công.</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var mission = await missionData.GetMissionAsync(id, tracking: true, cancellationToken);
        if (mission is null) return NotFound();
        var task = await missionData.GetTaskAsync(id, tracking: false, cancellationToken);
        if ((task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            || SimulationExecutionStore.TryGet(id, out _))
            return Conflict(new { message = "Gazebo completion state is transient; use the active execution callback." });
        mission.Complete();
        await missionData.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static MissionResponse ToResponse(Mission mission, string? robotName, Guid? robotId, string? runtimeId = null,
        string? statusOverride = null) =>
        new(mission.Id, mission.WorkflowId ?? Guid.Empty, mission.Name, mission.Description,
            statusOverride ?? (mission.Status == "Pending" ? "QUEUED" : mission.Status.ToUpperInvariant()), FormatPriority(mission.Priority),
            mission.CreatedAtUtc, mission.StartedAtUtc, mission.CompletedAtUtc, robotId, robotName, runtimeId);

    private async Task PublishLibrarianWorkflowNotificationAsync(
        Guid missionId,
        string runtimeId,
        MissionExecutionStateRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Status, "STEP_COMPLETED", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(request.StepType, "NOTIFY", StringComparison.OrdinalIgnoreCase)
            || request.Result is not JsonElement result
            || result.ValueKind != JsonValueKind.Object
            || !result.TryGetProperty("channel", out var channel)
            || channel.ValueKind != JsonValueKind.String
            || !string.Equals(channel.GetString(), "LIBRARIAN_WS", StringComparison.OrdinalIgnoreCase))
            return;

        var message = result.TryGetProperty("message", out var content) && content.ValueKind == JsonValueKind.String
            ? content.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(message)) return;
        var eventType = result.TryGetProperty("event_type", out var eventTypeValue)
            && eventTypeValue.ValueKind == JsonValueKind.String
                ? eventTypeValue.GetString()
                : null;

        var payload = result.TryGetProperty("payload", out var notificationPayload)
            && notificationPayload.ValueKind == JsonValueKind.Object
                ? notificationPayload
                : default;
        if (string.Equals(eventType, "PATROL_COMPLETED", StringComparison.OrdinalIgnoreCase))
        {
            var reportId = payload.ValueKind == JsonValueKind.Object && TryReadGuid(payload, "report_id", out var parsedReportId)
                ? parsedReportId
                : Guid.Empty;
            await hub.Clients.Group(RobotHub.LibrarianGroup).SendAsync("PATROL_COMPLETED", new
            {
                missionId,
                reportId,
                reportType = ReadString(payload, "report_type") ?? "PATROL",
                runtimeId,
                observationCount = ReadInt(payload, "observation_count"),
                fallenBookCount = ReadInt(payload, "fallen_book_count"),
                unknownObstacleCount = ReadInt(payload, "obstacle_count"),
                otherWarningCount = ReadInt(payload, "warning_count"),
                completedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        await hub.Clients.Group(RobotHub.LibrarianGroup).SendAsync("LibrarianNotification", new
        {
            type = eventType ?? "WORKFLOW_NOTIFICATION",
            runtimeId,
            missionId,
            executionId = request.ExecutionId,
            message,
            createdAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool RequiresRobotUi(IReadOnlyList<JsonElement> steps)
    {
        foreach (var step in steps)
        {
            var stepType = ReadString(step, "type")?.Trim().ToUpperInvariant();
            var parameters = step.ValueKind == JsonValueKind.Object
                && step.TryGetProperty("params", out var nestedParameters)
                && nestedParameters.ValueKind == JsonValueKind.Object
                    ? nestedParameters
                    : step;

            if (stepType == "DISPLAY") return true;
            if (stepType != "WAIT"
                || !string.Equals(ReadString(parameters, "wait_for"), "USER_TOUCH", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!parameters.TryGetProperty("show_ui", out var showUi)
                || showUi.ValueKind != JsonValueKind.False)
                return true;
        }

        return false;
    }

    private static int ReadInt(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : 0;

    private static bool TryReadGuid(JsonElement element, string propertyName, out Guid value)
    {
        value = Guid.Empty;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var item)
            && item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out value);
    }

    private static string? GetTransientSimulationStatus(Guid missionId, string? runtimeId)
    {
        if (runtimeId is not null && !string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)) return null;
        return SimulationExecutionStore.TryGet(missionId, out var snapshot) ? snapshot.Status : "QUEUED";
    }

    private async Task<Dictionary<string, object?>> ReadInitialContextAsync(Guid missionId, CancellationToken cancellationToken)
    {
        var raw = await missionData.GetInitialContextAsync(missionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw)) return new Dictionary<string, object?>();
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new Dictionary<string, object?>();
            return document.RootElement.EnumerateObject().ToDictionary(item => item.Name, item => (object?)item.Value.Clone());
        }
        catch (JsonException) { return new Dictionary<string, object?>(); }
    }

    private async Task<JobTask> CreateMissingJobTaskAsync(
        Guid missionId,
        Guid workflowId,
        string runtimeId,
        CancellationToken cancellationToken)
    {
        var jobTask = await missionData.CreateMissingJobTaskAsync(missionId, workflowId, runtimeId, cancellationToken);
        logger.LogInformation(
            "Created missing robot task {JobTaskId} for legacy mission {MissionId} on runtime {RuntimeId}.",
            jobTask.Id, missionId, runtimeId);
        return jobTask;
    }

    private static void RestoreLegacyGuidanceQuery(Mission mission, IDictionary<string, object?> initialContext)
    {
        if (initialContext.ContainsKey("query")) return;

        var prefix = mission.Name switch
        {
            "Search & Guide to Book" => "Reader search: ",
            "Search & Guide to Facility" => "Reader facility search: ",
            _ => null
        };
        if (prefix is null || mission.Description is not { } description
            || !description.StartsWith(prefix, StringComparison.Ordinal)) return;

        var query = description[prefix.Length..].Trim();
        if (query.Length > 0) initialContext["query"] = query;
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
