using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LiBot.Api.Hubs;
using LiBot.Api.Models;
using Microsoft.AspNetCore.SignalR;

namespace LiBot.Api.Services;

public sealed class RobotRuntimeService : BackgroundService
{
    private static readonly TimeSpan TelemetryFreshness = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ArmDuration = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, RuntimeState> _runtimes;
    private readonly IHubContext<RobotHub> _hub;
    private readonly ILogger<RobotRuntimeService> _logger;

    public RobotRuntimeService(
        IConfiguration configuration,
        IHubContext<RobotHub> hub,
        ILogger<RobotRuntimeService> logger)
    {
        _hub = hub;
        _logger = logger;
        _runtimes = new ConcurrentDictionary<string, RuntimeState>(
            StringComparer.OrdinalIgnoreCase);

        var configured = configuration.GetSection("RobotRuntimes")
            .Get<Dictionary<string, RuntimeEndpoint>>() ?? [];
        foreach (var (id, endpoint) in configured)
        {
            endpoint.Id = id.ToLowerInvariant();
            endpoint.DisplayName ??= id.Equals("real", StringComparison.OrdinalIgnoreCase)
                ? "LiBot thật"
                : "Gazebo mô phỏng";
            endpoint.Kind ??= id.Equals("real", StringComparison.OrdinalIgnoreCase) ? "REAL" : "SIM";
            _runtimes[id] = new RuntimeState(endpoint);
        }
    }

    public IReadOnlyList<RobotRuntimeResponse> GetAll() => _runtimes.Values
        .OrderBy(runtime => runtime.Endpoint.Id)
        .Select(ToResponse)
        .ToList();

    public RobotRuntimeResponse? Get(string runtimeId) =>
        _runtimes.TryGetValue(runtimeId, out var runtime) ? ToResponse(runtime) : null;

    public ArmRuntimeResponse? Arm(string runtimeId)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return null;
        lock (runtime.SyncRoot)
        {
            if (runtime.LastTelemetryAt is null
                || DateTimeOffset.UtcNow - runtime.LastTelemetryAt > TelemetryFreshness)
            {
                return null;
            }
            runtime.ArmLeaseId = Guid.NewGuid().ToString("N");
            runtime.ArmExpiresAt = DateTimeOffset.UtcNow.Add(ArmDuration);
            return new ArmRuntimeResponse(runtime.Endpoint.Id, runtime.ArmLeaseId, runtime.ArmExpiresAt.Value);
        }
    }

    public bool Disarm(string runtimeId)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return false;
        lock (runtime.SyncRoot)
        {
            runtime.ArmLeaseId = null;
            runtime.ArmExpiresAt = null;
        }
        return true;
    }

    public async Task<bool> UpdateTelemetryAsync(
        string runtimeId,
        RobotTelemetryRequest telemetry,
        CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return false;
        lock (runtime.SyncRoot)
        {
            runtime.Telemetry = telemetry with { Timestamp = telemetry.Timestamp.ToUniversalTime() };
            runtime.LastTelemetryAt = DateTimeOffset.UtcNow;
            runtime.MappingActive = telemetry.MappingActive;
            runtime.NavigationActive = telemetry.NavigationActive;
        }

        var snapshot = ToResponse(runtime);
        var group = RobotHub.RuntimeGroup(runtime.Endpoint.Id);
        await _hub.Clients.Group(group).SendAsync("RobotTelemetryUpdated", snapshot, cancellationToken);
        await _hub.Clients.Group(group).SendAsync(
            "RobotPositionUpdated",
            new
            {
                robotId = runtime.Endpoint.Id,
                telemetry.Pose.X,
                telemetry.Pose.Y,
                telemetry.Pose.Yaw,
            },
            cancellationToken);
        await _hub.Clients.Group(group).SendAsync(
            "NavigationStatusChanged",
            new { runtimeId = runtime.Endpoint.Id, telemetry.NavigationState, telemetry.ActiveGoalId, telemetry.Error },
            cancellationToken);
        if (telemetry.PlannedPath is { Count: > 0 })
        {
            await _hub.Clients.Group(group).SendAsync(
                "PlannedPathUpdated",
                new { runtimeId = runtime.Endpoint.Id, points = telemetry.PlannedPath },
                cancellationToken);
        }
        return true;
    }

    public async Task<bool> UpdateMapAsync(string runtimeId, OccupancyGridDto map, CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)
            || !string.Equals(runtime.Endpoint.Kind, "REAL", StringComparison.OrdinalIgnoreCase)
            || map.Width == 0 || map.Height == 0
            || map.Data is null
            || map.Data.Length != (long)map.Width * map.Height
            || !double.IsFinite(map.Resolution) || map.Resolution <= 0
            || !double.IsFinite(map.OriginX) || !double.IsFinite(map.OriginY) || !double.IsFinite(map.OriginYaw)
            || map.Data.Any(value => value is < -1 or > 100)) return false;
        lock (runtime.SyncRoot) runtime.Map = map;
        await _hub.Clients.Group(RobotHub.RuntimeGroup(runtime.Endpoint.Id))
            .SendAsync("RobotMapUpdated", map, cancellationToken);
        return true;
    }

    public OccupancyGridDto? GetLatestMap(string runtimeId)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return null;
        lock (runtime.SyncRoot) return runtime.Map;
    }

    public async Task<CommandResult> SendManualVelocityAsync(
        string runtimeId, ManualVelocityRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, request.ArmLeaseId, requireLocalized: false, out var runtime, out var error))
            return CommandResult.Fail(error!);
        if (!string.Equals(runtime!.Endpoint.Kind, "REAL", StringComparison.OrdinalIgnoreCase))
            return CommandResult.Fail("Manual mapping control is only available for the physical robot.");
        if (runtime.NavigationActive || string.Equals(runtime.Telemetry?.NavigationState, "NAVIGATING", StringComparison.OrdinalIgnoreCase))
            return CommandResult.Fail("Stop Nav2 before using manual control.");
        return await SendCommandAsync(runtime, new
        {
            command = "manual_velocity",
            linear_x = Math.Clamp(request.LinearX, -0.7, 0.7),
            linear_y = Math.Clamp(request.LinearY, -0.7, 0.7),
            angular_z = Math.Clamp(request.AngularZ, -0.5, 0.5),
        }, cancellationToken);
    }

    public async Task<CommandResult> StopManualAsync(string runtimeId, CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return CommandResult.Fail("Robot runtime was not found.");
        return await SendCommandAsync(runtime, new { command = "manual_stop" }, cancellationToken);
    }

    public async Task<CommandResult> SetMappingAsync(string runtimeId, bool enabled, string? leaseId, CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return CommandResult.Fail("Robot runtime was not found.");
        if (!string.Equals(runtime.Endpoint.Kind, "REAL", StringComparison.OrdinalIgnoreCase))
            return CommandResult.Fail("SLAM mapping is only available for the physical robot.");
        if (enabled)
        {
            if (!TryGetReadyRuntime(runtimeId, leaseId, requireLocalized: false, out runtime, out var error))
                return CommandResult.Fail(error!);
            if (runtime!.NavigationActive) return CommandResult.Fail("Stop Nav2 before starting SLAM mapping.");
        }
        var result = await SendCommandAsync(runtime, new { command = enabled ? "start_mapping" : "stop_mapping" }, cancellationToken);
        if (result.Success) runtime.MappingActive = enabled;
        return result;
    }

    public async Task<CommandResult> SaveMapOnRobotAsync(string runtimeId, string fileStem, string? leaseId, CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, leaseId, requireLocalized: false, out var runtime, out var error))
            return CommandResult.Fail(error!);
        if (runtime!.Map is null) return CommandResult.Fail("No live occupancy map has been received yet.");
        if (!runtime.MappingActive) return CommandResult.Fail("Start SLAM mapping before saving this map.");
        var result = await SendCommandAsync(runtime, new { command = "save_map", file_stem = fileStem }, cancellationToken);
        return result;
    }

    public async Task<CommandResult> StartNavigationModeAsync(string runtimeId, string mapPath, string? leaseId, CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, leaseId, requireLocalized: false, out var runtime, out var error))
            return CommandResult.Fail(error!);
        if (runtime!.MappingActive) return CommandResult.Fail("Stop SLAM mapping before starting Nav2.");
        var result = await SendCommandAsync(runtime, new { command = "start_navigation_mode", map_path = mapPath }, cancellationToken);
        if (result.Success) runtime.NavigationActive = true;
        return result;
    }

    public async Task<CommandResult> StopNavigationModeAsync(string runtimeId, CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return CommandResult.Fail("Robot runtime was not found.");
        var result = await SendCommandAsync(runtime, new { command = "stop_navigation_mode" }, cancellationToken);
        if (result.Success) runtime.NavigationActive = false;
        return result;
    }

    public async Task<CommandResult> SetInitialPoseAsync(
        string runtimeId,
        InitialPoseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, request.ArmLeaseId, requireLocalized: false, out var runtime, out var error))
            return CommandResult.Fail(error!);

        return await SendCommandAsync(runtime!, new
        {
            command = "set_initial_pose",
            pose = new { x = request.X, y = request.Y, yaw = request.Yaw },
        }, cancellationToken);
    }

    public async Task<CommandResult> StartNavigationAsync(
        string runtimeId,
        NavigationGoalRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, request.ArmLeaseId, requireLocalized: true, out var runtime, out var error))
            return CommandResult.Fail(error!);

        var commandId = Guid.NewGuid().ToString("N");
        var result = await SendCommandAsync(runtime!, new
        {
            command = "start_navigation",
            command_id = commandId,
            target = new { x = request.X, y = request.Y, theta = request.Yaw },
            speed_mode = request.SpeedMode,
            obstacle_policy = request.ObstaclePolicy,
            timeout_sec = request.TimeoutSec,
        }, cancellationToken);
        return result.Success ? result with { CommandId = commandId } : result;
    }

    public async Task<CommandResult> CancelNavigationAsync(
        string runtimeId,
        string commandId,
        string? armLeaseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, armLeaseId, requireLocalized: false, out var runtime, out var error))
            return CommandResult.Fail(error!);

        return await SendCommandAsync(runtime!, new
        {
            command = "cancel_navigation",
            command_id = commandId,
        }, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            foreach (var runtime in _runtimes.Values)
            {
                bool changed;
                lock (runtime.SyncRoot)
                {
                    var now = DateTimeOffset.UtcNow;
                    var telemetryStale = runtime.LastTelemetryAt is null
                        || now - runtime.LastTelemetryAt > TelemetryFreshness;
                    changed = runtime.ArmLeaseId is not null
                        && (runtime.ArmExpiresAt <= now || telemetryStale);
                    if (changed)
                    {
                        runtime.ArmLeaseId = null;
                        runtime.ArmExpiresAt = null;
                    }
                }
                if (changed)
                {
                    await _hub.Clients.Group(RobotHub.RuntimeGroup(runtime.Endpoint.Id))
                        .SendAsync("RuntimeConnectionChanged", ToResponse(runtime), stoppingToken);
                }
            }
        }
    }

    private bool TryGetReadyRuntime(
        string runtimeId,
        string? leaseId,
        bool requireLocalized,
        out RuntimeState? runtime,
        out string? error)
    {
        if (!_runtimes.TryGetValue(runtimeId, out runtime))
        {
            error = "Robot runtime was not found.";
            return false;
        }
        var response = ToResponse(runtime);
        if (!response.Connected || response.Stale)
        {
            error = "Robot telemetry is offline or stale.";
            return false;
        }
        if (string.Equals(runtime.Endpoint.Kind, "REAL", StringComparison.OrdinalIgnoreCase)
            && (!response.Armed || !string.Equals(runtime.ArmLeaseId, leaseId, StringComparison.Ordinal)))
        {
            error = "The real robot is not armed with a valid lease.";
            return false;
        }
        if (requireLocalized
            && !string.Equals(runtime.Telemetry?.LocalizationState, "LOCALIZED", StringComparison.OrdinalIgnoreCase))
        {
            error = "The robot must be localized before accepting a navigation goal.";
            return false;
        }
        error = null;
        return true;
    }

    private async Task<CommandResult> SendCommandAsync(
        RuntimeState runtime,
        object command,
        CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(runtime.Endpoint.Host, runtime.Endpoint.Port, cancellationToken);
            await using var stream = client.GetStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await writer.WriteLineAsync(JsonSerializer.Serialize(command).AsMemory(), cancellationToken);
            await writer.FlushAsync(cancellationToken);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var line = await reader.ReadLineAsync(timeout.Token);
            if (string.IsNullOrWhiteSpace(line)) return CommandResult.Fail("Robot gateway returned an empty response.");
            using var response = JsonDocument.Parse(line);
            var root = response.RootElement;
            var status = root.TryGetProperty("status", out var statusProperty)
                ? statusProperty.GetString()
                : null;
            if (status is "ok" or "accepted") return CommandResult.Ok(root.Clone());
            var message = root.TryGetProperty("message", out var messageProperty)
                ? messageProperty.GetString()
                : "Robot gateway rejected the command.";
            return CommandResult.Fail(message ?? "Robot gateway rejected the command.");
        }
        catch (Exception exception) when (exception is SocketException or IOException or JsonException or OperationCanceledException)
        {
            _logger.LogWarning(exception, "Robot runtime command failed for {RuntimeId}", runtime.Endpoint.Id);
            return CommandResult.Fail(exception is OperationCanceledException
                ? "Robot gateway command timed out."
                : "Robot gateway is unavailable.");
        }
    }

    private static RobotRuntimeResponse ToResponse(RuntimeState runtime)
    {
        lock (runtime.SyncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var stale = runtime.LastTelemetryAt is null || now - runtime.LastTelemetryAt > TelemetryFreshness;
            var armed = runtime.ArmLeaseId is not null && runtime.ArmExpiresAt > now;
            return new RobotRuntimeResponse(
                runtime.Endpoint.Id,
                runtime.Endpoint.DisplayName!,
                runtime.Endpoint.Kind!,
                !stale,
                stale,
                armed,
                runtime.MappingActive,
                runtime.NavigationActive,
                armed ? runtime.ArmExpiresAt : null,
                runtime.LastTelemetryAt,
                runtime.Telemetry);
        }
    }

    private sealed class RuntimeState(RuntimeEndpoint endpoint)
    {
        public RuntimeEndpoint Endpoint { get; } = endpoint;
        public object SyncRoot { get; } = new();
        public string? ArmLeaseId { get; set; }
        public DateTimeOffset? ArmExpiresAt { get; set; }
        public DateTimeOffset? LastTelemetryAt { get; set; }
        public RobotTelemetryRequest? Telemetry { get; set; }
        public OccupancyGridDto? Map { get; set; }
        public bool MappingActive { get; set; }
        public bool NavigationActive { get; set; }
    }

    private sealed class RuntimeEndpoint
    {
        public string Id { get; set; } = "";
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9000;
        public string? DisplayName { get; set; }
        public string? Kind { get; set; }
    }
}

public sealed record CommandResult(bool Success, string? Error, JsonElement? Payload, string? CommandId)
{
    public static CommandResult Ok(JsonElement payload) => new(true, null, payload, null);
    public static CommandResult Fail(string error) => new(false, error, null, null);
}

