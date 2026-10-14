using LiBot.Application.Workflow.FacilityGuidance;
using LiBot.Contracts.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/facility-guidance")]
public sealed class FacilityGuidanceController(IFacilityGuidanceService service) : ControllerBase
{
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

    [HttpGet("facilities/{facilityId:guid}")]
    public async Task<ActionResult<FacilityGuidanceDetail>> GetFacility(
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        var facility = await service.GetFacilityAsync(facilityId, cancellationToken);
        return facility is null ? NotFound("Facility not found.") : Ok(facility);
    }

    [HttpPost("feedback")]
    public async Task<ActionResult<FacilityFeedbackResponse>> SaveFeedback(
        FacilityFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5) return BadRequest("Rating must be between 1 and 5.");
        var feedback = await service.SaveFeedbackAsync(request, cancellationToken);
        if (feedback is null)
            return NotFound("Task was not found.");

        return Ok(feedback);
    }

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

    [HttpPost("start")]
    public async Task<IActionResult> Start(
        StartFacilityGuidanceRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest("A search query is required.");

        var baseUrl = string.IsNullOrWhiteSpace(request.BackendBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}"
            : request.BackendBaseUrl.TrimEnd('/');

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

    [HttpPost("events")]
    public async Task<IActionResult> SubmitEvent(
        FacilityGuidanceEventRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventType)) return BadRequest("eventType is required.");
        try
        {
            return Ok(await service.SubmitEventAsync(request, cancellationToken));
        }
        catch (FacilityGuidanceRobotUnavailableException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "LiBot is unavailable",
                detail: exception.Message);
        }
    }

    [HttpGet("executions/{executionId}")]
    public async Task<IActionResult> GetExecution(
        string executionId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetExecutionAsync(executionId, cancellationToken));
        }
        catch (FacilityGuidanceRobotUnavailableException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "LiBot is unavailable",
                detail: exception.Message);
        }
    }

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
}
