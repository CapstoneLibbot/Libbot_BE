using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Workflow.FacilityGuidance;
using LiBot.Application.Robot;
using LiBot.Contracts.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Tìm địa điểm trong thư viện và cung cấp luồng robot hướng dẫn người dùng đến đó.</summary>
[ApiController]
[Route("api/facility-guidance")]
public sealed class FacilityGuidanceController(
    IFacilityGuidanceService service,
    IJobDataService jobData,
    IRobotRuntimeService runtimes,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Tìm khu vực hoặc địa điểm theo tên (tùy chọn lọc theo mapId).</summary>
    [HttpGet("search")]
    public async Task<ActionResult<FacilitySearchResponse>> Search(
        [FromQuery] string? query = null,
        [FromQuery] string matchMode = "FUZZY",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? mapId = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(matchMode, "FUZZY", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase))
            return BadRequest("matchMode must be FUZZY or EXACT.");
        if (page < 1) return BadRequest("page must be at least 1.");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100.");
        if ((long)(page - 1) * pageSize > int.MaxValue) return BadRequest("page is too large.");

        return Ok(await service.SearchAsync(query, matchMode, page, pageSize, cancellationToken, mapId));
    }

    /// <summary>Lấy toàn bộ facility đang hoạt động cùng vị trí điều hướng.</summary>
    [HttpGet("facilities")]
    public async Task<ActionResult<IReadOnlyList<FacilityGuidanceDetail>>> GetAllFacilities(
        CancellationToken cancellationToken) =>
        Ok(await service.GetAllFacilitiesAsync(cancellationToken));

    /// <summary>Lấy vị trí và thông tin điều hướng của một địa điểm.</summary>
    [HttpGet("facilities/{facilityId:guid}")]
    public async Task<ActionResult<FacilityGuidanceDetail>> GetFacility(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var facility = await service.GetFacilityAsync(facilityId, cancellationToken);
        return facility is null ? NotFound("Facility not found.") : Ok(facility);
    }

    /// <summary>Lưu đánh giá sau nhiệm vụ hướng dẫn đến địa điểm.</summary>
    [HttpPost("feedback")]
    public async Task<ActionResult<FacilityFeedbackResponse>> SaveFeedback(
        FacilityFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
        var isSimulationTask = await service.IsSimulationTaskAsync(request.JobId, cancellationToken);
        if (isSimulationTask)
            return Ok(new FacilityFeedbackResponse(Guid.Empty, request.JobId, request.Rating));
        var feedback = await service.SaveFeedbackAsync(request, cancellationToken);
        if (feedback is null)
            return NotFound("Task was not found.");

        return CreatedAtAction(nameof(GetFeedbackById), new { feedbackId = feedback.FeedbackId }, feedback);
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<IReadOnlyList<FacilityFeedbackResponse>>> GetFeedback(
        [FromQuery] Guid? jobId, CancellationToken cancellationToken) =>
        Ok(await service.GetFeedbackAsync(jobId, cancellationToken));

    [HttpGet("feedback/{feedbackId:guid}")]
    public async Task<ActionResult<FacilityFeedbackResponse>> GetFeedbackById(Guid feedbackId, CancellationToken cancellationToken)
    {
        var feedback = await service.GetFeedbackByIdAsync(feedbackId, cancellationToken);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    [HttpPut("feedback/{feedbackId:guid}")]
    public async Task<ActionResult<FacilityFeedbackResponse>> UpdateFeedback(
        Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
        var feedback = await service.UpdateFeedbackAsync(feedbackId, request, cancellationToken);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    [HttpDelete("feedback/{feedbackId:guid}")]
    public async Task<IActionResult> DeleteFeedback(Guid feedbackId, CancellationToken cancellationToken) =>
        await service.DeleteFeedbackAsync(feedbackId, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Lấy workflow FACILITY_GUIDANCE đang active.</summary>
    [HttpGet("workflow")]
    public async Task<IActionResult> GetWorkflow(CancellationToken cancellationToken)
    {
        FacilityGuidanceLoadedWorkflow? loadedWorkflow;
        try
        {
            loadedWorkflow = await service.LoadWorkflowAsync(cancellationToken);
        }
        catch (FacilityGuidanceWorkflowConfigurationException exception)
        {
            return WorkflowConfigurationProblem(exception.Message);
        }

        if (loadedWorkflow is null)
        {
            return WorkflowConfigurationProblem(
                $"No active workflow with type '{FacilityGuidanceWorkflow.Type}' was found.");
        }

        return Ok(new
        {
            workflowId = loadedWorkflow.Id,
            type = loadedWorkflow.Type,
            name = loadedWorkflow.Name,
            steps = loadedWorkflow.Steps
        });
    }

    /// <summary>Bắt đầu nhiệm vụ robot hướng dẫn đến địa điểm đã tìm được.</summary>
    /// <remarks>Luồng Gazebo được khởi chạy qua job Run để giữ trạng thái mô phỏng tạm thời.</remarks>
    [HttpPost("start")]
    public async Task<IActionResult> Start(
        StartFacilityGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RuntimeId) || request.FacilityId is null)
            return BadRequest("runtimeId and facilityId are required.");

        var runtimeId = request.RuntimeId.Trim().ToLowerInvariant();
        var runtime = runtimes.Get(runtimeId);
        if (runtime is null)
            return BadRequest(new { message = $"Robot runtime '{runtimeId}' is not configured." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { code = GuidanceErrorCodes.SimUseRunButton, message = "Start Gazebo runs from the job Run button so execution state stays transient." });

        var facility = await service.GetFacilityAsync(request.FacilityId.Value, cancellationToken);
        if (facility is null) return NotFound("Facility not found.");
        var endpoint = facility.Location.Endpoint;
        if (!double.IsFinite(endpoint.X) || !double.IsFinite(endpoint.Y) || !double.IsFinite(endpoint.Theta))
            return UnprocessableEntity(new { code = GuidanceErrorCodes.InvalidEndpoint, message = "Selected facility has no valid navigation endpoint." });

        var runtimeError = runtimes.ValidateGuidanceStart(runtimeId, request.ArmLeaseId);
        if (runtimeError is not null)
            return Conflict(new { code = GuidanceErrorCodes.FromRuntimeValidation(runtimeError), message = runtimeError });
        if (!Guid.TryParse(runtime.Telemetry?.ActiveMapId, out var activeMapId))
            return Conflict(new { code = GuidanceErrorCodes.NoActiveMap, message = "The robot has no active map. Start navigation on the map containing this destination first." });
        if (activeMapId != facility.Location.MapId)
            return Conflict(new { code = GuidanceErrorCodes.WrongActiveMap, message = "The selected destination is not on the robot's active map." });
        if (!await jobData.MapExistsAsync(activeMapId, cancellationToken))
            return NotFound("The robot's active map was not found.");

        var robotId = request.RobotId;
        if (robotId == Guid.Empty)
        {
            robotId = await jobData.GetOrCreateRobotIdForMapAsync(
                activeMapId, runtimeId, runtime.DisplayName, cancellationToken);
        }
        else if (!await jobData.RobotExistsAsync(robotId, cancellationToken))
        {
            return NotFound("Robot not found.");
        }

        var queryName = !string.IsNullOrWhiteSpace(request.Query) ? request.Query.Trim() : facility.Name;

        var baseUrl = !string.IsNullOrWhiteSpace(request.BackendBaseUrl)
            ? request.BackendBaseUrl.TrimEnd('/')
            : configuration["LiBot:BackendBaseUrl"]?.TrimEnd('/')
                ?? $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        try
        {
            var normalizedRequest = request with { Query = queryName, RobotId = robotId, RuntimeId = runtimeId };
            var result = await service.StartAsync(normalizedRequest, baseUrl, cancellationToken);
            if (result.Workflow is null)
            {
                return WorkflowConfigurationProblem(
                    $"No active workflow with type '{FacilityGuidanceWorkflow.Type}' was found.");
            }

            if (!result.IsAccepted)
            {
                var message = GetString(result.Robot, "message") ?? GetString(result.Robot, "error")
                    ?? "Robot runtime did not accept the facility guidance workflow.";
                return UnprocessableEntity(new { code = GuidanceErrorCodes.RobotRejected, message, jobId = result.JobId, robot = result.Robot });
            }

            return Accepted(new
            {
                jobId = result.JobId,
                executionId = result.JobId,
                workflowId = result.Workflow.Id,
                runtimeId,
                robot = result.Robot
            });
        }
        catch (FacilityGuidanceWorkflowConfigurationException exception)
        {
            return WorkflowConfigurationProblem(exception.Message);
        }
        catch (FacilityGuidanceRobotUnavailableException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "LiBot is unavailable",
                detail: exception.Message);
        }
    }

    /// <summary>Gửi sự kiện tương tác cho lần chạy hướng dẫn địa điểm.</summary>
    [HttpPost("events")]
    public async Task<IActionResult> SubmitEvent(
        FacilityGuidanceEventRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventType)) return BadRequest("eventType is required.");
        if (string.IsNullOrWhiteSpace(request.ExecutionId)) return BadRequest("executionId is required.");
        try
        {
            var response = await service.SubmitEventAsync(request, cancellationToken);
            var status = GetString(response, "status");
            if (string.Equals(status, "accepted", StringComparison.OrdinalIgnoreCase))
                return Accepted(response);

            if (string.Equals(status, "error", StringComparison.OrdinalIgnoreCase))
            {
                var code = GetString(response, "code");
                return code is "INVALID_EVENT_DATA" or "INVALID_NAVIGATION_TARGET"
                    ? UnprocessableEntity(response)
                    : Conflict(response);
            }

            return StatusCode(StatusCodes.Status502BadGateway, response);
        }
        catch (FacilityGuidanceRobotUnavailableException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "LiBot is unavailable",
                detail: exception.Message);
        }
        catch (FacilityGuidanceExecutionNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    /// <summary>Lấy trạng thái lần chạy hướng dẫn địa điểm.</summary>
    [HttpGet("executions/{executionId}")]
    public async Task<IActionResult> GetExecution(
        string executionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.GetExecutionAsync(executionId, cancellationToken);
            return string.Equals(GetString(response, "status"), "error", StringComparison.OrdinalIgnoreCase)
                ? NotFound(response)
                : Ok(response);
        }
        catch (FacilityGuidanceRobotUnavailableException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "LiBot is unavailable",
                detail: exception.Message);
        }
        catch (FacilityGuidanceExecutionNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    /// <summary>Đánh dấu Job hướng dẫn địa điểm hoàn thành hoặc thất bại.</summary>
    [HttpPost("jobs/{jobId:guid}/complete")]
    public async Task<IActionResult> CompleteJob(
        Guid jobId,
        CompleteFacilityGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        return await service.CompleteTaskAsync(jobId, request, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private ObjectResult WorkflowConfigurationProblem(string detail) => Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Facility guidance workflow is not configured correctly",
        detail: detail);

    private static string? GetString(JsonElement value, string propertyName) =>
        value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
