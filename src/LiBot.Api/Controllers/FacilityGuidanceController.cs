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
    IRobotRuntimeService runtimes,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Tìm khu vực hoặc địa điểm theo tên.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<FacilitySearchResponse>> Search(
        [FromQuery] string query,
        [FromQuery] string matchMode = "FUZZY",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("A search query is required.");
        if (!string.Equals(matchMode, "FUZZY", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase))
            return BadRequest("matchMode must be FUZZY or EXACT.");

        return Ok(await service.SearchAsync(query, matchMode, cancellationToken));
    }

    /// <summary>Lấy toàn bộ facility đang hoạt động cùng vị trí điều hướng.</summary>
    [HttpGet("facilities")]
    public async Task<ActionResult<IReadOnlyList<FacilityGuidanceDetail>>> GetAllFacilities(
        CancellationToken cancellationToken) =>
        Ok(await service.GetAllFacilitiesAsync(cancellationToken));

    /// <summary>Lấy thông tin và vị trí điều hướng của phòng đọc sách.</summary>
    [HttpGet("facilities/reading-room")]
    public async Task<ActionResult<FacilityGuidanceDetail>> GetReadingRoom(CancellationToken cancellationToken)
    {
        var readingRoom = await service.GetReadingRoomAsync(cancellationToken);
        return readingRoom is null ? NotFound("Reading room facility not found.") : Ok(readingRoom);
    }

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
        var isSimulationTask = await service.IsSimulationTaskAsync(request.TaskId, cancellationToken);
        if (isSimulationTask)
            return Ok(new FacilityFeedbackResponse(Guid.Empty, request.TaskId, request.Rating));
        var feedback = await service.SaveFeedbackAsync(request, cancellationToken);
        if (feedback is null)
            return NotFound("Task was not found.");

        return CreatedAtAction(nameof(GetFeedbackById), new { feedbackId = feedback.FeedbackId }, feedback);
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<IReadOnlyList<FacilityFeedbackResponse>>> GetFeedback(
        [FromQuery] Guid? taskId, CancellationToken cancellationToken) =>
        Ok(await service.GetFeedbackAsync(taskId, cancellationToken));

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
    /// <remarks>Luồng Gazebo được khởi chạy qua mission Run để giữ trạng thái mô phỏng tạm thời.</remarks>
    [HttpPost("start")]
    public async Task<IActionResult> Start(
        StartFacilityGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest("A search query is required.");
        if (string.IsNullOrWhiteSpace(request.RuntimeId)) return BadRequest("runtimeId is required.");

        var runtimeId = request.RuntimeId.Trim().ToLowerInvariant();
        if (runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = $"Robot runtime '{runtimeId}' is not configured." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Start Gazebo runs from the mission Run button so execution state stays transient." });
        var runtimeError = runtimes.ValidateGuidanceStart(runtimeId, request.ArmLeaseId);
        if (runtimeError is not null) return Conflict(new { message = runtimeError });

        var baseUrl = !string.IsNullOrWhiteSpace(request.BackendBaseUrl)
            ? request.BackendBaseUrl.TrimEnd('/')
            : configuration["LiBot:BackendBaseUrl"]?.TrimEnd('/')
                ?? $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

        try
        {
            var result = await service.StartAsync(request, baseUrl, cancellationToken);
            if (result.Workflow is null)
            {
                return WorkflowConfigurationProblem(
                    $"No active workflow with type '{FacilityGuidanceWorkflow.Type}' was found.");
            }

            if (!result.IsAccepted)
            {
                return UnprocessableEntity(new { missionId = result.MissionId, robot = result.Robot });
            }

            return Accepted(new
            {
                missionId = result.MissionId,
                executionId = result.MissionId,
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

    /// <summary>Đánh dấu task hướng dẫn địa điểm hoàn thành hoặc thất bại.</summary>
    [HttpPost("tasks/{taskId:guid}/complete")]
    public async Task<IActionResult> CompleteTask(
        Guid taskId,
        CompleteFacilityGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        return await service.CompleteTaskAsync(taskId, request, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private ObjectResult WorkflowConfigurationProblem(string detail) => Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Facility guidance workflow is not configured correctly",
        detail: detail);

    private static string? GetString(System.Text.Json.JsonElement value, string propertyName) =>
        value.ValueKind == System.Text.Json.JsonValueKind.Object &&
        value.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == System.Text.Json.JsonValueKind.String
            ? property.GetString()
            : null;
}
