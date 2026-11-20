using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Infrastructure.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflow-data/inventory")]
public sealed class InventoryWorkflowController(
    IInventoryWorkflowService inventoryWorkflow,
    IWorkflowDataService workflowData) : ControllerBase
{
    [HttpPost("actions")]
    public async Task<IActionResult> ExecuteAction(
        InventoryWorkflowActionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.MissionId == Guid.Empty || string.IsNullOrWhiteSpace(request.RuntimeId)
            || string.IsNullOrWhiteSpace(request.ExecutionId) || string.IsNullOrWhiteSpace(request.Action)
            || request.Payload.ValueKind != System.Text.Json.JsonValueKind.Object
            || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new { message = "missionId, runtimeId, executionId, action, object payload and idempotencyKey are required." });

        var runtimeId = request.RuntimeId.Trim().ToLowerInvariant();
        if (runtimeId is not ("real" or "sim"))
            return BadRequest(new { message = "runtimeId must be 'real' or 'sim'." });

        var mission = await workflowData.GetMissionAsync(request.MissionId, cancellationToken);
        var activeSimulation = runtimeId == "sim"
            && SimulationExecutionStore.TryGetExecution(request.MissionId, out var activeExecutionId, out var activeRuntime)
            && string.Equals(activeRuntime, "sim", StringComparison.OrdinalIgnoreCase)
            && string.Equals(activeExecutionId, request.ExecutionId, StringComparison.Ordinal);
        if (mission is null && !activeSimulation) return NotFound(new { message = "Mission task not found." });
        if (mission is not null && !string.Equals(mission.RuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime does not match the mission." });
        if (runtimeId == "sim" && !activeSimulation)
            return Conflict(new { message = "No active in-memory simulation exists for this mission execution." });

        try
        {
            var normalizedRequest = request with { RuntimeId = runtimeId, Action = request.Action.Trim() };
            return Ok(await inventoryWorkflow.ExecuteAsync(normalizedRequest, runtimeId == "sim", cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("reports/{missionId:guid}")]
    public async Task<IActionResult> GetReport(
        Guid missionId,
        [FromQuery] string runtimeId = "real",
        CancellationToken cancellationToken = default)
    {
        if (missionId == Guid.Empty || runtimeId is not ("real" or "sim"))
            return BadRequest(new { message = "A valid missionId and runtimeId ('real' or 'sim') are required." });

        var mission = await workflowData.GetMissionAsync(missionId, cancellationToken);
        var activeSimulation = runtimeId == "sim"
            && SimulationExecutionStore.TryGetExecution(missionId, out _, out var activeRuntime)
            && string.Equals(activeRuntime, "sim", StringComparison.OrdinalIgnoreCase);
        if (mission is null && !activeSimulation) return NotFound(new { message = "Mission task not found." });
        if (mission is not null && !string.Equals(mission.RuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime does not match the mission." });

        var report = await inventoryWorkflow.GetReportAsync(missionId, runtimeId, cancellationToken);
        return report is null ? NotFound(new { message = "No inventory report exists for this mission." }) : Ok(report);
    }
}
