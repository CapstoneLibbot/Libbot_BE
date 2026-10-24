using Microsoft.AspNetCore.Mvc;
using LiBot.Application.Robot;
using LiBot.Infrastructure.Robot;
using System.Text.Json;

namespace LiBot.Api.Controllers;

/// <summary>Telemetry và lệnh điều khiển cho runtime robot thật hoặc Gazebo.</summary>
/// <remarks>Bao gồm tốc độ mô phỏng, khóa điều khiển, map trực tiếp, manual drive, mapping và navigation.</remarks>
[ApiController]
[Route("api/robot-runtimes")]
public sealed class RobotTelemetryController(
    IRobotRuntimeService runtimes,
    ILiBotTcpClient tcpClient,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<RobotTelemetryController> logger) : ControllerBase
{
    /// <summary>Lấy danh sách runtime robot đã cấu hình cùng trạng thái kết nối.</summary>
    [HttpGet]
    public ActionResult<IReadOnlyList<RobotRuntimeResponse>> GetAll() => Ok(runtimes.GetAll());

    /// <summary>Lấy trạng thái và telemetry mới nhất của một runtime.</summary>
    [HttpGet("{runtimeId}")]
    public ActionResult<RobotRuntimeResponse> Get(string runtimeId)
    {
        var runtime = runtimes.Get(runtimeId);
        return runtime is null ? NotFound() : Ok(runtime);
    }

    /// <summary>Lấy tốc độ hiện tại của Gazebo SIM.</summary>
    [HttpGet("{runtimeId}/simulation-speed")]
    public async Task<IActionResult> GetSimulationSpeed(string runtimeId, CancellationToken cancellationToken)
    {
        if (!string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Simulation speed controls are available only for the SIM runtime." });
        try
        {
            using var response = await tcpClient.SendAsync(runtimeId, new { command = "get_simulation_speed" }, cancellationToken);
            var payload = response.RootElement.Clone();
            return HasStatus(payload, "ok") ? Ok(payload) : Conflict(new { message = ReadMessage(payload) ?? "Unable to read Gazebo speed." });
        }
        catch (LiBotTcpException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = exception.Message });
        }
    }

    /// <summary>Đặt tốc độ Gazebo SIM từ 1x đến 4x.</summary>
    [HttpPost("{runtimeId}/simulation-speed")]
    public async Task<IActionResult> SetSimulationSpeed(
        string runtimeId,
        SimulationSpeedRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Simulation speed controls are available only for the SIM runtime." });
        if (request.Multiplier is < 1 or > 4)
            return BadRequest(new { message = "Simulation speed must be 1x, 2x, 3x, or 4x." });
        try
        {
            using var response = await tcpClient.SendAsync(runtimeId, new
            {
                command = "set_simulation_speed",
                multiplier = request.Multiplier
            }, cancellationToken);
            var payload = response.RootElement.Clone();
            return HasStatus(payload, "accepted") ? Accepted(payload) : Conflict(new { message = ReadMessage(payload) ?? "Gazebo rejected the speed request." });
        }
        catch (LiBotTcpException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = exception.Message });
        }
    }

    /// <summary>Xin lease điều khiển runtime khi robot đang kết nối và telemetry còn mới.</summary>
    [HttpPost("{runtimeId}/arm")]
    public ActionResult<ArmRuntimeResponse> Arm(string runtimeId)
    {
        var lease = runtimes.Arm(runtimeId);
        return lease is null
            ? Conflict(new { message = "Runtime is unavailable or its telemetry is stale." })
            : Ok(lease);
    }

    /// <summary>Nhả lease điều khiển runtime.</summary>
    [HttpDelete("{runtimeId}/arm")]
    public IActionResult Disarm(string runtimeId) => runtimes.Disarm(runtimeId) ? NoContent() : NotFound();

    /// <summary>Nhận telemetry định kỳ do Pi hoặc runtime gửi lên.</summary>
    [HttpPost("{runtimeId}/telemetry")]
    public async Task<IActionResult> Telemetry(
        string runtimeId,
        RobotTelemetryRequest request,
        CancellationToken cancellationToken) =>
        await runtimes.UpdateTelemetryAsync(runtimeId, request, cancellationToken) ? Accepted() : NotFound();

    /// <summary>Lấy occupancy grid mới nhất của runtime.</summary>
    [HttpGet("{runtimeId}/map")]
    public IActionResult GetMap(string runtimeId)
    {
        var map = runtimes.GetLatestMap(runtimeId);
        return map is null ? NoContent() : Ok(map);
    }

    /// <summary>Cập nhật occupancy grid trực tiếp do runtime gửi.</summary>
    [HttpPost("{runtimeId}/map")]
    public async Task<IActionResult> Map(string runtimeId, OccupancyGridDto request, CancellationToken cancellationToken)
    {
        if (await runtimes.UpdateMapAsync(runtimeId, request, cancellationToken)) return Accepted();
        logger.LogWarning(
            "Rejected occupancy grid from runtime {RuntimeId}: {Width}x{Height}, {CellCount} cells, resolution {Resolution}.",
            runtimeId, request.Width, request.Height, request.Data?.Length ?? 0, request.Resolution);
        return BadRequest(new { message = "Invalid occupancy grid." });
    }

    [HttpGet("{runtimeId}/camera/stream")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task CameraStream(string runtimeId, CancellationToken cancellationToken)
    {
        if (runtimes.Get(runtimeId) is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            await Response.WriteAsJsonAsync(new { message = "Robot runtime was not found." }, cancellationToken);
            return;
        }

        var section = configuration.GetSection($"RobotRuntimes:{runtimeId}");
        var host = section["Host"];
        var port = section.GetValue("CameraStreamPort", 9012);
        if (string.IsNullOrWhiteSpace(host)
            || Uri.CheckHostName(host) == UriHostNameType.Unknown
            || port is < 1 or > 65535)
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await Response.WriteAsJsonAsync(new { message = "Camera stream is not configured for this runtime." }, cancellationToken);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new UriBuilder(Uri.UriSchemeHttp, host, port, "/stream.mjpg").Uri);
            var token = section["CameraStreamToken"];
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var upstream = await httpClientFactory.CreateClient("RobotCamera").SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!upstream.IsSuccessStatusCode)
            {
                Response.StatusCode = StatusCodes.Status502BadGateway;
                await Response.WriteAsJsonAsync(
                    new { message = $"Robot camera returned HTTP {(int)upstream.StatusCode}." },
                    cancellationToken);
                return;
            }

            Response.StatusCode = StatusCodes.Status200OK;
            Response.ContentType = upstream.Content.Headers.ContentType?.ToString()
                ?? "multipart/x-mixed-replace; boundary=frame";
            Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
            Response.Headers.Pragma = "no-cache";
            await upstream.Content.CopyToAsync(Response.Body, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The browser closed the live feed; there is no response left to write.
        }
        catch (Exception exception) when (Response.HasStarted)
        {
            logger.LogDebug(exception, "Camera stream for runtime {RuntimeId} ended after it started.", runtimeId);
        }
        catch (Exception exception) when (!Response.HasStarted)
        {
            logger.LogWarning(exception, "Unable to proxy camera stream for runtime {RuntimeId}.", runtimeId);
            Response.StatusCode = StatusCodes.Status502BadGateway;
            await Response.WriteAsJsonAsync(new { message = "Unable to connect to the robot camera." }, cancellationToken);
        }
    }

    /// <summary>Send linear/angular velocity to a robot in manual control mode.</summary>
    [HttpPost("{runtimeId}/manual-velocity")]
    public async Task<IActionResult> ManualVelocity(string runtimeId, ManualVelocityRequest request, CancellationToken cancellationToken)
    {
        var result = await runtimes.SendManualVelocityAsync(runtimeId, request, cancellationToken);
        if (result.Success) return Accepted(result.Payload);
        if (result.Error?.Contains("did not acknowledge", StringComparison.OrdinalIgnoreCase) == true)
            return StatusCode(502, new { message = result.Error });
        return Conflict(new { message = result.Error });
    }

    /// <summary>Dừng lệnh manual hiện tại; có thể yêu cầu tiếp tục navigation.</summary>
    [HttpPost("{runtimeId}/manual-stop")]
    public async Task<IActionResult> ManualStop(
        string runtimeId,
        CancellationToken cancellationToken,
        [FromQuery] bool resumeNavigation = false)
    {
        var result = await runtimes.StopManualAsync(runtimeId, resumeNavigation, cancellationToken);
        if (result.Success) return Accepted(result.Payload);
        if (result.Error?.Contains("did not acknowledge", StringComparison.OrdinalIgnoreCase) == true)
            return StatusCode(502, new { message = result.Error });
        return Conflict(new { message = result.Error });
    }

    /// <summary>Bật hoặc tắt chế độ mapping của robot.</summary>
    [HttpPost("{runtimeId}/mapping")]
    public async Task<IActionResult> Mapping(string runtimeId, [FromQuery] bool enabled, [FromQuery] string? armLeaseId, CancellationToken cancellationToken)
    {
        var result = await runtimes.SetMappingAsync(runtimeId, enabled, armLeaseId, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    /// <summary>Dừng chế độ navigation trên robot.</summary>
    [HttpDelete("{runtimeId}/navigation-mode")]
    public async Task<IActionResult> StopNavigationMode(string runtimeId, [FromQuery] string? armLeaseId, CancellationToken cancellationToken)
    {
        var result = await runtimes.StopNavigationModeAsync(runtimeId, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    /// <summary>Đặt pose ban đầu của robot trên map hiện tại.</summary>
    [HttpPost("{runtimeId}/initial-pose")]
    public async Task<IActionResult> SetInitialPose(
        string runtimeId,
        InitialPoseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await runtimes.SetInitialPoseAsync(runtimeId, request, cancellationToken);
        return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
    }

    /// <summary>Gửi goal điều hướng cho robot và trả command ID khi được nhận.</summary>
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

    /// <summary>Hủy goal điều hướng đã gửi trước đó.</summary>
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

    private static bool HasStatus(JsonElement payload, string expected) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty("status", out var status)
        && status.ValueKind == JsonValueKind.String
        && string.Equals(status.GetString(), expected, StringComparison.OrdinalIgnoreCase);

    private static string? ReadMessage(JsonElement payload) =>
        payload.ValueKind == JsonValueKind.Object
        && payload.TryGetProperty("message", out var message)
        && message.ValueKind == JsonValueKind.String
            ? message.GetString()
            : null;
}

public sealed record SimulationSpeedRequest(int Multiplier);

