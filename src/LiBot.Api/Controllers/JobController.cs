using System.Text.Json;
using LiBot.Api.Hubs;
using LiBot.Application.Robot;
using LiBot.Domain.Job;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Tạo và quản lý job; khởi chạy workflow robot và cung cấp trạng thái thực thi.</summary>
/// <remarks>Job trên runtime SIM giữ trạng thái thực thi tạm thời; job trên runtime REAL được lưu bền vững.</remarks>
[ApiController]
[Route("api/jobs")]
public class JobController(
    IJobDataService jobData,
    IWorkflowDocumentService workflowDocuments,
    ILiBotTcpClient tcpClient,
    IRobotRuntimeService runtimes,
    IHubContext<RobotHub> hub,
    ILogger<JobController> logger) : ControllerBase
{
    /// <summary>Lấy danh sách job cùng robot, runtime và trạng thái hiện tại.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JobResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var jobs = await jobData.GetAllJobsAsync(cancellationToken);
        var responses = new List<JobResponse>();
        foreach (var job in jobs)
        {
            var task = await jobData.GetTaskAsync(job.Id, tracking: false, cancellationToken);
            var robotName = await jobData.GetRobotNameAsync(job.RobotId, cancellationToken);
            responses.Add(ToResponse(job, robotName, job.RobotId, task?.RuntimeId,
                GetTransientSimulationStatus(job.Id, task?.RuntimeId, job.Status)));
        }
        return Ok(responses);
    }

    /// <summary>Lấy thông tin một job theo ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(id, tracking: false, cancellationToken);
        if (job is null) return NotFound();
        var task = await jobData.GetTaskAsync(id, tracking: false, cancellationToken);
        var robot = await jobData.GetRobotNameAsync(job.RobotId, cancellationToken);
        return Ok(ToResponse(job, robot, job.RobotId, task?.RuntimeId,
            GetTransientSimulationStatus(job.Id, task?.RuntimeId, job.Status)));
    }

    /// <summary>Tạo job gắn với workflow và runtime được chọn.</summary>
    [HttpPost]
    public async Task<ActionResult<JobResponse>> Create(JobRequest request, CancellationToken cancellationToken)
    {
        if (request.InitialContext is { } suppliedContext && suppliedContext.ValueKind != JsonValueKind.Object)
            return BadRequest(new { message = "initialContext must be a JSON object." });
        if (request.WorkflowId is null || !Guid.TryParse(request.WorkflowId, out var workflowId))
            return BadRequest("A valid workflowId is required.");
        var workflow = await jobData.GetWorkflowAsync(workflowId, tracking: false, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        if (!workflow.IsActive) return Conflict("The workflow is inactive.");
        var runtimeId = (request.RuntimeId ?? "real").Trim().ToLowerInvariant();
        var runtime = runtimes.Get(runtimeId);
        if (runtimeId is not ("sim" or "real") || runtime is null)
            return BadRequest(new { message = "runtimeId must identify a configured 'sim' or 'real' runtime." });
        Guid? robotId = Guid.TryParse(request.RobotId, out var suppliedRobotId)
            ? suppliedRobotId
            : null;
        if (robotId is null || robotId == Guid.Empty)
        {
            var mapId = ReadMapId(request.InitialContext);
            if (mapId is null
                && Guid.TryParse(runtime.Telemetry?.ActiveMapId, out var activeMapId))
                mapId = activeMapId;

            if (mapId is null)
                return BadRequest("A valid robotId is required, and no job or active runtime map is available to resolve it.");
            if (!await jobData.MapExistsAsync(mapId.Value, cancellationToken))
                return NotFound("Job map was not found.");

            robotId = await jobData.GetOrCreateRobotIdForMapAsync(
                mapId.Value, runtimeId, runtime.DisplayName, cancellationToken);
        }
        if (!await jobData.RobotExistsAsync(robotId.Value, cancellationToken))
            return NotFound("Robot not found.");
        var job = new Job(request.Name ?? workflow.Name, request.Description ?? workflow.Description,
            robotId.Value, workflowId, priority: ParsePriority(request.Priority));
        job.Update(job.Name, job.Description, job.Priority, workflowId);
        var initialContextJson = request.InitialContext is { ValueKind: JsonValueKind.Object } initialContext
            ? initialContext.GetRawText()
            : null;
        RobotWorkflowDocument workflowDocument;
        try
        {
            workflowDocument = await workflowDocuments.BuildRobotDocumentAsync(workflowId, runtimeId, cancellationToken)
                ?? throw new InvalidWorkflowDefinitionException("Workflow was not found.");
        }
        catch (InvalidWorkflowDefinitionException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        await jobData.CreateJobAsync(job, workflowDocument, runtimeId, initialContextJson, cancellationToken);
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            SimulationExecutionStore.TryStoreInitialContext(job.Id, initialContextJson);

        var robotName = await jobData.GetRobotNameAsync(robotId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ToResponse(job, robotName, robotId, runtimeId));
    }

    private static Guid? ReadMapId(JsonElement? initialContext)
    {
        if (!initialContext.HasValue || initialContext.Value.ValueKind != JsonValueKind.Object)
            return null;
        var context = initialContext.Value;

        if (!context.TryGetProperty("map_id", out var mapIdElement)
            && !context.TryGetProperty("mapId", out mapIdElement))
            return null;

        return mapIdElement.ValueKind == JsonValueKind.String
               && Guid.TryParse(mapIdElement.GetString(), out var mapId)
            ? mapId
            : null;
    }

    /// <summary>Xem trước task/step của workflow trước khi tạo hoặc chạy job.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<JobPreviewResponse>> Preview(JobPreviewRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.WorkflowId, out var workflowId)) return BadRequest("A valid workflowId is required.");
        var workflow = await jobData.GetWorkflowAsync(workflowId, tracking: false, cancellationToken);
        if (workflow is null) return NotFound("Workflow not found.");
        var steps = await jobData.GetWorkflowStepsAsync(workflowId, cancellationToken);
        Guid? robotId = Guid.TryParse(request.RobotId, out var parsedRobotId) ? parsedRobotId : null;
        Guid? mapId = Guid.TryParse(request.MapId, out var parsedMapId) ? parsedMapId : null;
        if (mapId.HasValue && !await jobData.MapExistsAsync(mapId.Value, cancellationToken))
            return NotFound("Map not found.");
        return Ok(new JobPreviewResponse(workflowId, robotId, mapId,
            new[] { new Point(0, 0), new Point(1, 1) }, steps, 1, 30));
    }

    /// <summary>Cập nhật mô tả, ưu tiên, điểm bắt đầu hoặc điểm đến của job.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<JobResponse>> Update(Guid id, JobUpdateRequest request, CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        Guid? workflowId = null;
        if (!string.IsNullOrWhiteSpace(request.WorkflowId))
        {
            if (!Guid.TryParse(request.WorkflowId, out var parsedWorkflowId)) return BadRequest("Invalid workflowId.");
            if (!await jobData.WorkflowExistsAsync(parsedWorkflowId, cancellationToken)) return NotFound("Workflow not found.");
            workflowId = parsedWorkflowId;
        }
        if (workflowId.HasValue && workflowId != job.WorkflowId)
            return Conflict("A job's workflow cannot be changed after creation.");
        job.Update(request.Name ?? job.Name, request.Description ?? job.Description,
            ParsePriority(request.Priority), job.WorkflowId);
        await jobData.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(job, null, null));
    }

    /// <summary>Xóa job theo ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        await jobData.DeleteJobAsync(job, cancellationToken);
        return NoContent();
    }

    /// <summary>Hủy job đang chạy và gửi lệnh hủy đến runtime robot.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        var task = await jobData.GetTaskAsync(id, tracking: true, cancellationToken);
        await AdoptSimulationExecutionIfNeededAsync(id, task?.ExecutionId, cancellationToken);
        if ((task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            || SimulationExecutionStore.TryGet(id, out _))
        {
            if (!SimulationExecutionStore.TryGetExecution(id, out var simulationExecutionId, out var simulationRuntimeId))
                return Conflict("Simulation job has not been started.");
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
                jobId = id,
                executionId = simulationExecutionId,
                runtimeId = simulationRuntimeId,
                status = "CANCELLED",
                updatedAt = DateTime.UtcNow
            };
            if (!SimulationExecutionStore.TryUpdate(id, simulationExecutionId, cancelledState))
                return Conflict("Simulation job has already finished.");
            if (SimulationExecutionStore.TryGet(id, out var cancelledSimulation) && cancelledSimulation.PersistJobStatus)
                await PersistSimulationJobStatusAsync(id, "CANCELLED", cancellationToken);
            await hub.Clients.All.SendAsync("JobStatusChanged", new { jobId = id, status = "CANCELLED" }, cancellationToken);
            await hub.Clients.All.SendAsync("JobStepChanged", cancelledState, cancellationToken);
            return NoContent();
        }
        if (task?.ExecutionId is string executionId && job.Status == "Running")
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
        try { job.Cancel(); }
        catch (InvalidOperationException exception) { return Conflict(exception.Message); }
        if (task is not null)
            task.UpdateExecutionState(JsonSerializer.Serialize(new { jobId = id, executionId = task.ExecutionId,
                runtimeId = task.RuntimeId, status = "CANCELLED", updatedAt = DateTime.UtcNow }));
        await jobData.SetJobTaskStatusesAsync(id, "Cancelled", cancellationToken);
        await jobData.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("JobStatusChanged", new { jobId = id, status = "CANCELLED" }, cancellationToken);
        return NoContent();
    }

    /// <summary>Chạy workflow của job trên runtime SIM hoặc REAL.</summary>
    /// <remarks>Với SIM, trạng thái được giữ trong bộ nhớ và không cập nhật trạng thái job trong database.</remarks>
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> Start(Guid id, [FromBody] JobStartRequest? request, CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        if (job.WorkflowId == Guid.Empty) return Conflict("Job has no workflow.");
        var jobTask = await jobData.GetTaskAsync(id, tracking: true, cancellationToken);
        var isRetry = job.Status is "Failed" or "Cancelled";
        var requestedRuntimeId = string.IsNullOrWhiteSpace(request?.RuntimeId)
            ? null
            : request.RuntimeId.Trim().ToLowerInvariant();
        if (jobTask is not null && requestedRuntimeId is not null
            && !string.Equals(jobTask.RuntimeId, requestedRuntimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime id does not match the job's assigned runtime." });
        var runtimeId = jobTask?.RuntimeId ?? requestedRuntimeId ?? "sim";
        var isSimulation = string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase);

        // A simulation run is transient: the job row remains QUEUED in the database,
        // so its run eligibility and prior run's ExecutionId must not gate a new click.
        if (!isSimulation && (job.Status == "Running" || job.Status == "Completed"))
            return Conflict("Only queued, failed, or cancelled jobs can start.");
        if (!isSimulation && jobTask?.ExecutionId is not null && !isRetry)
            return Conflict("Job has already been dispatched.");
        if (runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = $"Robot runtime '{runtimeId}' is not configured." });

        var preflightError = runtimes.ValidateGuidanceStart(runtimeId, request?.ArmLeaseId);
        if (preflightError is not null) return Conflict(new { message = preflightError });

        if (jobTask is null)
            return Conflict(new { message = "Job has no stored task configuration." });
        var document = await jobData.GetRobotWorkflowDocumentAsync(id, cancellationToken);
        if (document is null)
            return Conflict(new { message = "Job has missing or invalid step configuration." });

        var executionId = Guid.NewGuid().ToString("N");
        var initialContext = await ReadInitialContextAsync(id, cancellationToken);
        RestoreLegacyGuidanceQuery(job, initialContext);
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
            logger.LogWarning(exception, "Workflow preflight failed for job {JobId} on {RuntimeId}.", id, runtimeId);
            return Conflict(new { message = exception.Message });
        }
        if (!HasStatus(preflightResponse, "ok"))
            return Conflict(new { message = ReadMessage(preflightResponse) ?? "Robot preflight failed." });

        var acceptedState = new
        {
            jobId = id,
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
                return Conflict(new { message = "This simulation job is already running." });
        }
        else
        {
            try
            {
                if (isRetry)
                {
                    job.Retry();
                    await jobData.PrepareJobForRetryAsync(id, cancellationToken);
                }
                job.Start();
                jobTask!.Start();
                jobTask.SetExecutionId(executionId);
                jobTask.UpdateExecutionState(JsonSerializer.Serialize(acceptedState));
                await jobData.SaveChangesAsync(cancellationToken);
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
                workflow_type = document.Type,
                job_id = id.ToString(),
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
                    jobId = id, executionId, runtimeId, status = "FAILED",
                    error = ReadMessage(payload), updatedAt = DateTime.UtcNow
                };
                if (isSimulation) SimulationExecutionStore.TryUpdate(id, executionId, failedState);
                else
                {
                    job.Fail();
                    jobTask!.UpdateExecutionState(JsonSerializer.Serialize(failedState));
                    await jobData.SetJobTaskStatusesAsync(id, "Failed", cancellationToken);
                    await jobData.SaveChangesAsync(cancellationToken);
                }
                return Conflict(new { message = ReadMessage(payload) ?? "Robot rejected the workflow." });
            }
            return Accepted(new { jobId = id, executionId, runtimeId, status = "ACCEPTED" });
        }
        catch (LiBotTcpException exception)
        {
            var failedState = new
            {
                jobId = id, executionId, runtimeId, status = "FAILED",
                error = exception.Message, updatedAt = DateTime.UtcNow
            };
            if (isSimulation) SimulationExecutionStore.TryUpdate(id, executionId, failedState);
            else
            {
                job.Fail();
                jobTask!.UpdateExecutionState(JsonSerializer.Serialize(failedState));
                await jobData.SetJobTaskStatusesAsync(id, "Failed", cancellationToken);
                await jobData.SaveChangesAsync(cancellationToken);
            }
            return Conflict(new { message = exception.Message });
        }
    }

    /// <summary>Lấy trạng thái lần chạy hiện tại, ví dụ QUEUED, RUNNING, WAITING hoặc SUCCEEDED.</summary>
    [HttpGet("{id:guid}/execution")]
    public async Task<IActionResult> GetExecution(Guid id, CancellationToken cancellationToken)
    {
        var task = await jobData.GetTaskAsync(id, tracking: false, cancellationToken);
        await AdoptSimulationExecutionIfNeededAsync(id, task?.ExecutionId, cancellationToken);
        if (SimulationExecutionStore.TryGet(id, out var simulation)) return Ok(simulation.CurrentState);
        if (task is null)
        {
            var exists = await jobData.JobExistsAsync(id, cancellationToken);
            return exists
                ? Ok(new { jobId = id, runtimeId = "sim", executionId = (string?)null, status = "QUEUED" })
                : NotFound();
        }
        if (string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Ok(new { jobId = id, runtimeId = task.RuntimeId, executionId = (string?)null, status = "QUEUED" });
        if (string.IsNullOrWhiteSpace(task.ExecutionState))
            return Ok(new { jobId = id, runtimeId = task.RuntimeId, executionId = task.ExecutionId, status = "QUEUED" });
        try
        {
            using var document = JsonDocument.Parse(task.ExecutionState);
            return Ok(document.RootElement.Clone());
        }
        catch (JsonException)
        {
            return Ok(new { jobId = id, runtimeId = task.RuntimeId, executionId = task.ExecutionId, status = "UNKNOWN" });
        }
    }

    /// <summary>Lấy lịch sử cập nhật step của lần chạy job.</summary>
    [HttpGet("{id:guid}/execution/events")]
    public async Task<IActionResult> GetExecutionEvents(Guid id, CancellationToken cancellationToken)
    {
        var task = await jobData.GetTaskAsync(id, tracking: false, cancellationToken);
        await AdoptSimulationExecutionIfNeededAsync(id, task?.ExecutionId, cancellationToken);
        if (SimulationExecutionStore.TryGet(id, out var simulation)) return Ok(simulation.Events);
        if (task is null || string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Ok(Array.Empty<JsonElement>());
        var executionId = task.ExecutionId;
        if (string.IsNullOrWhiteSpace(executionId)) return Ok(Array.Empty<JsonElement>());
        var stateDocuments = await jobData.GetWorkflowStepEventDataAsync(id, cancellationToken);
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
        JobExecutionEventRequest request,
        CancellationToken cancellationToken)
    {
        var task = await jobData.GetTaskAsync(id, tracking: true, cancellationToken);
        await AdoptSimulationExecutionIfNeededAsync(id, task?.ExecutionId, cancellationToken);
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
                return Conflict("Simulation job has not started in this API session.");
            if (string.IsNullOrWhiteSpace(task.ExecutionId)) return Conflict("Job has not started.");
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
        JobExecutionStateRequest request,
        CancellationToken cancellationToken)
    {
        var supportedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "QUEUED", "ACCEPTED", "RUNNING", "WAITING", "STEP_COMPLETED", "STEP_FAILED", "SUCCEEDED", "COMPLETED", "FAILED", "CANCELLED"
        };
        if (string.IsNullOrWhiteSpace(request.Status) || !supportedStatuses.Contains(request.Status))
            return BadRequest(new { message = "Unsupported workflow execution status." });

        await AdoptSimulationExecutionIfNeededAsync(id, request.ExecutionId, cancellationToken);
        if (SimulationExecutionStore.TryGet(id, out var simulation))
        {
            if (!string.IsNullOrWhiteSpace(request.RuntimeId)
                && !string.Equals(simulation.RuntimeId, request.RuntimeId, StringComparison.OrdinalIgnoreCase))
                return Conflict(new { message = "Runtime id does not match the active simulation." });
            var simulationState = new
            {
                jobId = id,
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
            await hub.Clients.All.SendAsync("JobStepChanged", simulationState, cancellationToken);
            return Accepted(simulationState);
        }

        var task = await jobData.GetTaskAsync(id, tracking: true, cancellationToken);
        if (task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase)
            || task is null && string.Equals(request.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "No active in-memory simulation exists for this job." });
        if (task is null)
        {
            var legacyJob = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
            if (legacyJob is null) return NotFound();
            if (legacyJob.Status == "Pending") legacyJob.Start();
            task = await jobData.CreateMissingJobTaskAsync(
                id, legacyJob.WorkflowId, request.RuntimeId ?? "real", cancellationToken);
            task.Start();
            if (!string.IsNullOrWhiteSpace(request.ExecutionId)) task.SetExecutionId(request.ExecutionId);
            await jobData.SaveChangesAsync(cancellationToken);
        }
        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        if (job.Status is "Completed" or "Failed" or "Cancelled")
            return Accepted(new { jobId = id, executionId = task.ExecutionId, runtimeId = task.RuntimeId, status = job.Status.ToUpperInvariant() });
        if (!string.IsNullOrWhiteSpace(request.ExecutionId)
            && !string.Equals(task.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
            return Conflict(new { message = "Execution id does not match the active job." });
        if (!string.IsNullOrWhiteSpace(request.RuntimeId)
            && !string.Equals(task.RuntimeId, request.RuntimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime id does not match the assigned job runtime." });

        if (string.Equals(request.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
        {
            if (job.Status == "Pending") job.Start();
            if (!task.StartedAtUtc.HasValue) task.Start();
        }

        var state = new
        {
            jobId = id,
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
        if (request.Status.Equals("STEP_COMPLETED", StringComparison.OrdinalIgnoreCase)
            || request.Status.Equals("STEP_FAILED", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.StepId))
                return BadRequest(new { message = "stepId is required for a completed or failed step." });
            try
            {
                var recorded = await jobData.RecordWorkflowStepResultAsync(id, request.StepId, request.Status,
                    JsonSerializer.Serialize(state), cancellationToken);
                if (!recorded) await jobData.SaveChangesAsync(cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { message = exception.Message });
            }
        }
        else
        {
            await jobData.SaveChangesAsync(cancellationToken);
        }
        await PublishLibrarianWorkflowNotificationAsync(id, task.RuntimeId, request, cancellationToken);
        await hub.Clients.All.SendAsync("JobStepChanged", state, cancellationToken);
        return Accepted(state);
    }

    /// <summary>Nhận callback kết thúc workflow và cập nhật trạng thái job.</summary>
    [HttpPost("{id:guid}/execution/completed")]
    public async Task<IActionResult> CompleteExecution(
        Guid id,
        JobExecutionCompletionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "status must be SUCCEEDED, COMPLETED, FAILED, or CANCELLED." });
        var normalizedStatus = request.Status.Trim().ToUpperInvariant();
        if (normalizedStatus is not ("SUCCEEDED" or "COMPLETED" or "FAILED" or "CANCELLED"))
            return BadRequest(new { message = "status must be SUCCEEDED, COMPLETED, FAILED, or CANCELLED." });

        await AdoptSimulationExecutionIfNeededAsync(id, request.ExecutionId, cancellationToken);
        if (SimulationExecutionStore.TryGet(id, out var simulation))
        {
            if (!string.IsNullOrWhiteSpace(request.ExecutionId)
                && !string.Equals(simulation.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
                return Conflict(new { message = "Execution id does not match the active simulation." });
            var simulationStatus = normalizedStatus is "SUCCEEDED" or "COMPLETED" ? "SUCCEEDED" : normalizedStatus;
            var simulationState = new
            {
                jobId = id,
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
            if (simulation.PersistJobStatus)
                await PersistSimulationJobStatusAsync(id, simulationStatus, cancellationToken);
            await hub.Clients.All.SendAsync("JobStatusChanged", new { jobId = id, status = simulationStatus }, cancellationToken);
            await hub.Clients.All.SendAsync("JobStepChanged", simulationState, cancellationToken);
            return Accepted(simulationState);
        }

        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        var task = await jobData.GetTaskAsync(id, tracking: true, cancellationToken);
        if ((task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            || task is null)
            return Conflict(new { message = "No active in-memory simulation exists for this job." });
        if (task is null)
        {
            var legacySucceeded = normalizedStatus is "SUCCEEDED" or "COMPLETED";
            if (job.Status is not ("Completed" or "Failed" or "Cancelled"))
            {
                if (normalizedStatus == "CANCELLED") job.Cancel();
                else if (legacySucceeded) job.Complete();
                else job.Fail();
            }
            await jobData.SaveChangesAsync(cancellationToken);
            await hub.Clients.All.SendAsync("JobStatusChanged", new { jobId = id, status = normalizedStatus }, cancellationToken);
            return Accepted(new { jobId = id, status = normalizedStatus });
        }
        if (!string.IsNullOrWhiteSpace(request.ExecutionId)
            && !string.Equals(task.ExecutionId, request.ExecutionId, StringComparison.Ordinal))
            return Conflict(new { message = "Execution id does not match the active job." });

        var cancelled = normalizedStatus == "CANCELLED";
        var succeeded = normalizedStatus is "SUCCEEDED" or "COMPLETED";
        if (job.Status is not ("Completed" or "Failed" or "Cancelled"))
        {
            if (cancelled) job.Cancel();
            else if (succeeded) job.Complete();
            else job.Fail();
        }
        try { task.Complete(); } catch (InvalidOperationException) { }
        var state = new
        {
            jobId = id,
            executionId = task.ExecutionId,
            runtimeId = task.RuntimeId,
            status = cancelled ? "CANCELLED" : succeeded ? "SUCCEEDED" : "FAILED",
            currentStep = (string?)null,
            currentStepType = (string?)null,
            error = request.Message,
            updatedAt = DateTime.UtcNow
        };
        task.UpdateExecutionState(JsonSerializer.Serialize(state));
        await jobData.SetJobTaskStatusesAsync(id, cancelled ? "Cancelled" : succeeded ? "Completed" : "Failed",
            cancellationToken);
        await jobData.SaveChangesAsync(cancellationToken);
        await hub.Clients.All.SendAsync("JobStatusChanged", new { jobId = id, status = state.status }, cancellationToken);
        await hub.Clients.All.SendAsync("JobStepChanged", state, cancellationToken);
        return Accepted(state);
    }

    /// <summary>Đánh dấu job hoàn thành thủ công.</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(id, tracking: true, cancellationToken);
        if (job is null) return NotFound();
        var task = await jobData.GetTaskAsync(id, tracking: false, cancellationToken);
        if ((task is not null && string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            || SimulationExecutionStore.TryGet(id, out _))
            return Conflict(new { message = "Gazebo completion state is transient; use the active execution callback." });
        job.Complete();
        await jobData.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static JobResponse ToResponse(Job job, string? robotName, Guid? robotId, string? runtimeId = null,
        string? statusOverride = null) =>
        new(job.Id, job.WorkflowId, job.Name, job.Description,
            statusOverride ?? (job.Status == "Pending" ? "QUEUED" : job.Status.ToUpperInvariant()), FormatPriority(job.Priority),
            job.CreatedAtUtc, job.StartedAtUtc, job.CompletedAtUtc, robotId, robotName, runtimeId);

    private async Task PublishLibrarianWorkflowNotificationAsync(
        Guid jobId,
        string runtimeId,
        JobExecutionStateRequest request,
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
                jobId,
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
            jobId,
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

    private static string? GetTransientSimulationStatus(Guid jobId, string? runtimeId, string jobStatus)
    {
        if (runtimeId is not null && !string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)) return null;
        return SimulationExecutionStore.TryGet(jobId, out var snapshot)
            ? snapshot.Status
            : jobStatus == "Running" ? "RUNNING" : "QUEUED";
    }

    private async Task AdoptSimulationExecutionIfNeededAsync(
        Guid jobId,
        string? expectedExecutionId,
        CancellationToken cancellationToken)
    {
        if (SimulationExecutionStore.TryGet(jobId, out _)) return;
        var task = await jobData.GetTaskAsync(jobId, tracking: false, cancellationToken);
        if (task is null || !string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(task.ExecutionId)
            || !string.IsNullOrWhiteSpace(expectedExecutionId)
                && !string.Equals(task.ExecutionId, expectedExecutionId, StringComparison.Ordinal))
            return;

        var job = await jobData.GetJobAsync(jobId, tracking: false, cancellationToken);
        if (job?.Status != "Running") return;
        var acceptedState = new
        {
            jobId,
            executionId = task.ExecutionId,
            runtimeId = task.RuntimeId,
            status = "ACCEPTED",
            currentStep = (string?)null,
            currentStepType = (string?)null,
            expectedEventType = (string?)null,
            allowedValues = Array.Empty<string>(),
            updatedAt = DateTime.UtcNow
        };
        SimulationExecutionStore.TryStart(jobId, task.ExecutionId, task.RuntimeId, acceptedState,
            persistJobStatus: true);
    }

    private async Task PersistSimulationJobStatusAsync(
        Guid jobId,
        string status,
        CancellationToken cancellationToken)
    {
        var job = await jobData.GetJobAsync(jobId, tracking: true, cancellationToken);
        if (job is null) return;
        if (status is "SUCCEEDED" or "COMPLETED") job.Complete();
        else if (status == "CANCELLED") job.Cancel();
        else job.Fail();
        await jobData.SetJobTaskStatusesAsync(jobId,
            status is "SUCCEEDED" or "COMPLETED" ? "Completed" : status == "CANCELLED" ? "Cancelled" : "Failed",
            cancellationToken);
        await jobData.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, object?>> ReadInitialContextAsync(Guid jobId, CancellationToken cancellationToken)
    {
        if (SimulationExecutionStore.TryGetInitialContext(jobId, out var simulationContext))
            return ParseInitialContext(simulationContext);
        var raw = await jobData.GetInitialContextAsync(jobId, cancellationToken);
        return ParseInitialContext(raw);
    }

    private static Dictionary<string, object?> ParseInitialContext(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new Dictionary<string, object?>();
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return new Dictionary<string, object?>();
            return document.RootElement.EnumerateObject().ToDictionary(item => item.Name, item => (object?)item.Value.Clone());
        }
        catch (JsonException) { return new Dictionary<string, object?>(); }
    }

    private static void RestoreLegacyGuidanceQuery(Job job, IDictionary<string, object?> initialContext)
    {
        if (initialContext.ContainsKey("query")) return;

        var prefix = job.Name switch
        {
            "Search & Guide to Book" => "Reader search: ",
            "Search & Guide to Facility" => "Reader facility search: ",
            _ => null
        };
        if (prefix is null || job.Description is not { } description
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
