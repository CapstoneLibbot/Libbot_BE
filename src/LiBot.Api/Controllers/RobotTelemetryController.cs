using LiBot.Api.Models;
using LiBot.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/robot-runtimes")]
public sealed class RobotTelemetryController(RobotRuntimeService runtimes) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<RobotRuntimeResponse>> GetAll() => Ok(runtimes.GetAll());

    [HttpGet("{runtimeId}")]
    public ActionResult<RobotRuntimeResponse> Get(string runtimeId)
    {
        var runtime = runtimes.Get(runtimeId);
        return runtime is null ? NotFound() : Ok(runtime);
    }

    [HttpPost("{runtimeId}/arm")]
    public ActionResult<ArmRuntimeResponse> Arm(string runtimeId)
    {
        var lease = runtimes.Arm(runtimeId);
        return lease is null
            ? Conflict(new { message = "Runtime is unavailable or its telemetry is stale." })
            : Ok(lease);
    }

    [HttpDelete("{runtimeId}/arm")]
    public IActionResult Disarm(string runtimeId) => runtimes.Disarm(runtimeId) ? NoContent() : NotFound();

    [HttpPost("{runtimeId}/telemetry")]
    public async Task<IActionResult> Telemetry(
        string runtimeId,
        RobotTelemetryRequest request,
        CancellationToken cancellationToken) =>
        await runtimes.UpdateTelemetryAsync(runtimeId, request, cancellationToken) ? Accepted() : NotFound();

    [HttpPost("{runtimeId}/map")]
    public async Task<IActionResult> Map(string runtimeId, OccupancyGridDto request, CancellationToken cancellationToken) =>
        await runtimes.UpdateMapAsync(runtimeId, request, cancellationToken) ? Accepted() : BadRequest(new { message = "Invalid occupancy grid." });

    [HttpPost("{runtimeId}/manual-velocity")]
    public async Task<IActionResult> ManualVelocity(string runtimeId, ManualVelocityRequest request, CancellationToken cancellationToken)
    {
        var result = await runtimes.SendManualVelocityAsync(runtimeId, request, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    [HttpPost("{runtimeId}/manual-stop")]
    public async Task<IActionResult> ManualStop(string runtimeId, CancellationToken cancellationToken)
    {
        var result = await runtimes.StopManualAsync(runtimeId, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    [HttpPost("{runtimeId}/mapping")]
    public async Task<IActionResult> Mapping(string runtimeId, [FromQuery] bool enabled, [FromQuery] string? armLeaseId, CancellationToken cancellationToken)
    {
        var result = await runtimes.SetMappingAsync(runtimeId, enabled, armLeaseId, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    [HttpDelete("{runtimeId}/navigation-mode")]
    public async Task<IActionResult> StopNavigationMode(string runtimeId, [FromQuery] string? armLeaseId, CancellationToken cancellationToken)
    {
        var result = await runtimes.StopNavigationModeAsync(runtimeId, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    [HttpPost("{runtimeId}/initial-pose")]
    public async Task<IActionResult> SetInitialPose(
        string runtimeId,
        InitialPoseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await runtimes.SetInitialPoseAsync(runtimeId, request, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    [HttpPost("{runtimeId}/navigation-goals")]
    public async Task<IActionResult> Navigate(
        string runtimeId,
        NavigationGoalRequest request,
        CancellationToken cancellationToken)
    {
        var result = await runtimes.StartNavigationAsync(runtimeId, request, cancellationToken);
        return result.Success
            ? Accepted(new NavigationGoalResponse(runtimeId, result.CommandId!, "ACCEPTED"))
            : Conflict(new { message = result.Error });
    }

    [HttpDelete("{runtimeId}/navigation-goals/{commandId}")]
    public async Task<IActionResult> Cancel(
        string runtimeId,
        string commandId,
        [FromQuery] string? armLeaseId,
        CancellationToken cancellationToken)
    {
        var result = await runtimes.CancelNavigationAsync(runtimeId, commandId, armLeaseId, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }
}

