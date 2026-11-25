using LiBot.Application.Interfaces;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Contracts.Workflow;
using LiBot.Domain.Enums;
using LiBot.Infrastructure.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Tìm kiếm sách, xem vị trí bản sách và chạy quy trình hướng dẫn người đọc.</summary>
[ApiController]
[Route("api/book-guidance")]
public sealed class BookGuidanceController(
    IBookGuidanceService service,
    IJobDataService jobData,
    IRobotRuntimeService runtimes,
    IEndpointService endpoints,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Tìm đầu sách theo tên, ISBN, tác giả hoặc danh mục.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<BookSearchResponse>> Search(
        [FromQuery] string? query = null,
        [FromQuery] string? category = null,
        [FromQuery] string matchMode = "FUZZY",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(matchMode, "FUZZY", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(matchMode, "EXACT", StringComparison.OrdinalIgnoreCase))
            return BadRequest("matchMode must be FUZZY or EXACT.");
        if (page < 1) return BadRequest("page must be at least 1.");
        if (pageSize is < 1 or > 100) return BadRequest("pageSize must be between 1 and 100.");
        if ((long)(page - 1) * pageSize > int.MaxValue) return BadRequest("page is too large.");

        return Ok(await service.SearchAsync(query, category, matchMode, page, pageSize, cancellationToken));
    }

    /// <summary>Lấy thông tin sách, các bản sách còn sẵn và vị trí trên kệ.</summary>
    [HttpGet("books/{bookId:guid}")]
    public async Task<ActionResult<BookGuidanceDetail>> GetBook(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await service.GetBookAsync(bookId, cancellationToken);
        return book is null ? NotFound("Book not found.") : Ok(book);
    }

    /// <summary>Lưu đánh giá của người dùng sau một nhiệm vụ hướng dẫn sách.</summary>
    [HttpPost("feedback")]
    public async Task<ActionResult<FeedbackResponse>> SaveFeedback(FeedbackRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
        var isSimulationTask = await service.IsSimulationTaskAsync(request.JobId, cancellationToken);
        if (isSimulationTask)
            return Ok(new FeedbackResponse(Guid.Empty, request.JobId, request.Rating));
        var feedback = await service.SaveFeedbackAsync(request, cancellationToken);
        if (feedback is null)
            return NotFound("Task was not found.");

        return CreatedAtAction(nameof(GetFeedbackById), new { feedbackId = feedback.FeedbackId }, feedback);
    }

    [HttpGet("feedback")]
    public async Task<ActionResult<IReadOnlyList<FeedbackResponse>>> GetFeedback(
        [FromQuery] Guid? jobId, CancellationToken cancellationToken) =>
        Ok(await service.GetFeedbackAsync(jobId, cancellationToken));

    [HttpGet("feedback/{feedbackId:guid}")]
    public async Task<ActionResult<FeedbackResponse>> GetFeedbackById(Guid feedbackId, CancellationToken cancellationToken)
    {
        var feedback = await service.GetFeedbackByIdAsync(feedbackId, cancellationToken);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    [HttpPut("feedback/{feedbackId:guid}")]
    public async Task<ActionResult<FeedbackResponse>> UpdateFeedback(
        Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
        var feedback = await service.UpdateFeedbackAsync(feedbackId, request, cancellationToken);
        return feedback is null ? NotFound() : Ok(feedback);
    }

    [HttpDelete("feedback/{feedbackId:guid}")]
    public async Task<IActionResult> DeleteFeedback(Guid feedbackId, CancellationToken cancellationToken) =>
        await service.DeleteFeedbackAsync(feedbackId, cancellationToken) ? NoContent() : NotFound();

    /// <summary>Lấy workflow BOOK_GUIDANCE đang active để hiển thị hoặc kiểm tra cấu hình.</summary>
    [HttpGet("workflow")]
    public async Task<IActionResult> GetWorkflow(CancellationToken cancellationToken)
    {
        BookGuidanceLoadedWorkflow? loadedWorkflow;
        try
        {
            loadedWorkflow = await service.LoadWorkflowAsync(cancellationToken);
        }
        catch (BookGuidanceWorkflowConfigurationException exception)
        {
            return WorkflowConfigurationProblem(exception.Message);
        }

        if (loadedWorkflow is null)
        {
            return WorkflowConfigurationProblem(
                $"No active workflow with type '{BookGuidanceWorkflow.Type}' was found.");
        }

        return Ok(new
        {
            workflowId = loadedWorkflow.Id,
            type = loadedWorkflow.Type,
            name = loadedWorkflow.Name,
            steps = loadedWorkflow.Steps
        });
    }

    /// <summary>Bắt đầu nhiệm vụ robot tìm và hướng dẫn đến bản sách đã chọn.</summary>
    /// <remarks>Luồng Gazebo được khởi chạy từ job Run để trạng thái mô phỏng không ghi vào database.</remarks>
    [HttpPost("start")]
    public async Task<IActionResult> Start(StartBookGuidanceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RuntimeId))
            return BadRequest("runtimeId is required.");
        if (string.Equals(request.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { code = GuidanceErrorCodes.SimUseRunButton, message = "Start Gazebo runs from the job Run button so execution state stays transient." });

        Guid targetEndpointId;
        Guid targetMapId;
        string queryName;

        if (request.BookId.HasValue && request.BookItemId.HasValue)
        {
            var target = await service.GetCopyStartTargetAsync(
                request.BookId.Value,
                request.BookItemId.Value,
                cancellationToken);
            if (target is null) return NotFound("Book not found.");
            if (target.CopyStatus is null) return NotFound("Selected book copy was not found.");
            if (!string.Equals(target.CopyStatus, nameof(BookItemStatus.Available), StringComparison.OrdinalIgnoreCase))
                return Conflict(new { code = GuidanceErrorCodes.CopyUnavailable, message = "Selected book copy is not available." });
            var endpoint = target.Endpoint;
            if (endpoint is null || !double.IsFinite(endpoint.X) || !double.IsFinite(endpoint.Y) || !double.IsFinite(endpoint.Theta))
                return UnprocessableEntity(new { code = GuidanceErrorCodes.InvalidEndpoint, message = "Selected book copy has no valid navigation endpoint." });
            if (request.EndpointId.HasValue && request.EndpointId.Value != endpoint.EndpointId)
                return Conflict(new { code = GuidanceErrorCodes.EndpointMismatch, message = "The selected endpoint does not match the book copy location." });

            targetEndpointId = endpoint.EndpointId;
            targetMapId = endpoint.MapId;
            queryName = !string.IsNullOrWhiteSpace(request.Query) ? request.Query.Trim() : target.BookTitle;
        }
        else if (request.EndpointId.HasValue)
        {
            var ep = await endpoints.GetByIdAsync(request.EndpointId.Value, null, cancellationToken);
            if (ep is null) return NotFound("Endpoint not found.");
            if (!ep.IsActive || !double.IsFinite(ep.PositionX) || !double.IsFinite(ep.PositionY) || !double.IsFinite(ep.Theta))
                return UnprocessableEntity(new { code = GuidanceErrorCodes.InvalidEndpoint, message = "Selected endpoint is not active or has invalid coordinates." });

            targetEndpointId = ep.Id;
            targetMapId = ep.MapId;
            queryName = !string.IsNullOrWhiteSpace(request.Query) ? request.Query.Trim() : ep.Name;
        }
        else
        {
            return BadRequest("Either (bookId, bookItemId) or endpointId is required.");
        }

        var runtimeError = runtimes.ValidateGuidanceStart(request.RuntimeId, request.ArmLeaseId);
        if (runtimeError is not null)
            return Conflict(new { code = GuidanceErrorCodes.FromRuntimeValidation(runtimeError), message = runtimeError });
        var runtime = runtimes.Get(request.RuntimeId);
        if (runtime is null) return BadRequest("The selected robot runtime was not found.");
        if (!Guid.TryParse(runtime.Telemetry?.ActiveMapId, out var activeMapId))
            return Conflict(new { code = GuidanceErrorCodes.NoActiveMap, message = "The robot has no active map. Start navigation on the map containing this destination first." });
        if (activeMapId != targetMapId)
            return Conflict(new { code = GuidanceErrorCodes.WrongActiveMap, message = "The selected destination is not on the robot's active map." });
        if (!await jobData.MapExistsAsync(activeMapId, cancellationToken))
            return NotFound("The robot's active map was not found.");

        var robotId = request.RobotId;
        if (robotId == Guid.Empty)
        {
            robotId = await jobData.GetOrCreateRobotIdForMapAsync(
                activeMapId, request.RuntimeId, runtime.DisplayName, cancellationToken);
        }
        else if (!await jobData.RobotExistsAsync(robotId, cancellationToken))
        {
            return NotFound("Robot not found.");
        }
        request = request with { Query = queryName, RobotId = robotId, EndpointId = targetEndpointId };

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
                    $"No active workflow with type '{BookGuidanceWorkflow.Type}' was found.");
            }

            if (!result.IsAccepted)
            {
                return UnprocessableEntity(new { code = GuidanceErrorCodes.RobotRejected, message = "Robot runtime did not accept the book guidance workflow.", jobId = result.JobId, robot = result.Robot });
            }

            return Accepted(new
            {
                jobId = result.JobId,
                executionId = result.JobId,
                workflowId = result.Workflow.Id,
                runtimeId = request.RuntimeId,
                robot = result.Robot
            });
        }
        catch (BookGuidanceWorkflowConfigurationException exception)
        {
            return WorkflowConfigurationProblem(exception.Message);
        }
        catch (BookGuidanceRobotUnavailableException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "LiBot is unavailable", detail: exception.Message);
        }
    }

    /// <summary>Gửi câu trả lời hoặc sự kiện tương tác cho một lần chạy hướng dẫn.</summary>
    [HttpPost("events")]
    public async Task<IActionResult> SubmitEvent(BookGuidanceEventRequest request, CancellationToken cancellationToken)
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
        catch (BookGuidanceRobotUnavailableException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "LiBot is unavailable", detail: exception.Message);
        }
        catch (BookGuidanceExecutionNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    /// <summary>Lấy trạng thái hiện tại của lần chạy hướng dẫn.</summary>
    [HttpGet("executions/{executionId}")]
    public async Task<IActionResult> GetExecution(string executionId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await service.GetExecutionAsync(executionId, cancellationToken);
            return string.Equals(GetString(response, "status"), "error", StringComparison.OrdinalIgnoreCase)
                ? NotFound(response)
                : Ok(response);
        }
        catch (BookGuidanceRobotUnavailableException exception)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "LiBot is unavailable", detail: exception.Message);
        }
        catch (BookGuidanceExecutionNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    /// <summary>Đánh dấu Job hướng dẫn hoàn thành hoặc thất bại.</summary>
    [HttpPost("jobs/{jobId:guid}/complete")]
    public async Task<IActionResult> CompleteJob(Guid jobId, CompleteBookGuidanceRequest request, CancellationToken cancellationToken)
    {
        return await service.CompleteTaskAsync(jobId, request, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private ObjectResult WorkflowConfigurationProblem(string detail) => Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Book guidance workflow is not configured correctly",
        detail: detail);

    private static string? GetString(System.Text.Json.JsonElement value, string propertyName) =>
        value.ValueKind == System.Text.Json.JsonValueKind.Object &&
        value.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == System.Text.Json.JsonValueKind.String
            ? property.GetString()
            : null;
}
