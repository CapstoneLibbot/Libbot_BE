using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiBot.Infrastructure.RobotRuntime;

public sealed class RobotRuntimeService : BackgroundService, IRobotRuntimeService
{
    private static readonly TimeSpan TelemetryFreshness = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan ArmDuration = TimeSpan.FromMinutes(5);
    private const int MaxFirmwareWheelSpeedMmS = 1000;
    private readonly ConcurrentDictionary<string, RuntimeState> _runtimes;
    private readonly IRobotRuntimeEventPublisher _eventPublisher;
    private readonly ILogger<RobotRuntimeService> _logger;

    public RobotRuntimeService(
        IConfiguration configuration,
        IRobotRuntimeEventPublisher eventPublisher,
        ILogger<RobotRuntimeService> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
        _runtimes = new ConcurrentDictionary<string, RuntimeState>(
            StringComparer.OrdinalIgnoreCase);

        var configured = configuration.GetSection("RobotRuntimes")
            .Get<Dictionary<string, RuntimeEndpoint>>() ?? [];
        if (!configured.ContainsKey("real"))
        {
            throw new InvalidOperationException(
                "Robot runtime 'real' must be configured in the BE .env file.");
        }

        foreach (var (id, endpoint) in configured)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Host)
                || Uri.CheckHostName(endpoint.Host) == UriHostNameType.Unknown
                || endpoint.Port is < 1 or > 65535)
            {
                throw new InvalidOperationException(
                    $"Robot runtime '{id}' must define a valid Host and Port in the BE .env file.");
            }

            endpoint.Id = id.ToLowerInvariant();
            endpoint.DisplayName ??= id.Equals("real", StringComparison.OrdinalIgnoreCase)
                ? "LiBot thật"
                : "Gazebo mô phỏng";
            endpoint.Kind ??= id.Equals("real", StringComparison.OrdinalIgnoreCase) ? "REAL" : "SIM";
            if (string.Equals(endpoint.Kind, "REAL", StringComparison.OrdinalIgnoreCase)
                && endpoint.MaxWheelSpeedMmS is < 1 or > MaxFirmwareWheelSpeedMmS)
            {
                throw new InvalidOperationException(
                    $"Robot runtime '{id}' must configure MaxWheelSpeedMmS between 1 and {MaxFirmwareWheelSpeedMmS}, " +
                    "matching the current STM32 firmware limit.");
            }
            _runtimes[id] = new RuntimeState(endpoint);
        }
    }

    public IReadOnlyList<RobotRuntimeResponse> GetAll() => _runtimes.Values
        .OrderBy(runtime => runtime.Endpoint.Id)
        .Select(ToResponse)
        .ToList();

    public RobotRuntimeResponse? Get(string runtimeId) =>
        _runtimes.TryGetValue(runtimeId, out var runtime) ? ToResponse(runtime) : null;

    public string? ValidateGuidanceStart(string runtimeId, string? armLeaseId)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime))
            return "Robot runtime was not found.";

        var response = ToResponse(runtime);
        if (!response.Connected || response.Stale)
            return "Robot runtime is offline or its telemetry is stale.";
        if (!string.Equals(runtime.Telemetry?.LocalizationState, "LOCALIZED", StringComparison.OrdinalIgnoreCase))
            return "Robot must be localized before starting guidance.";
        if (runtime.Telemetry?.NavigationReady != true)
            return "Nav2 is not ready. Set the initial pose and wait for activation.";
        if (string.Equals(runtime.Endpoint.Kind, "REAL", StringComparison.OrdinalIgnoreCase)
            && (!response.Armed || !string.Equals(runtime.ArmLeaseId, armLeaseId, StringComparison.Ordinal)))
            return "Arm the physical robot and provide its current lease before starting guidance.";

        return null;
    }

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
        await _eventPublisher.PublishTelemetryUpdatedAsync(runtime.Endpoint.Id, snapshot, telemetry, cancellationToken);
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
        await _eventPublisher.PublishMapUpdatedAsync(runtime.Endpoint.Id, map, cancellationToken);
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
        var result = await SendCommandAsync(runtime, new
        {
            command = "manual_velocity",
            linear_x = Math.Clamp(request.LinearX, -0.7, 0.7),
            linear_y = Math.Clamp(request.LinearY, -0.7, 0.7),
            angular_z = Math.Clamp(request.AngularZ, -0.5, 0.5),
            max_wheel_speed_mm_s = runtime.Endpoint.MaxWheelSpeedMmS,
        }, cancellationToken);
        if (!result.Success) return result;
        return HasStm32Acknowledgement(result)
            ? result
            : CommandResult.Fail("Robot gateway accepted the velocity, but STM32 did not acknowledge it.");
    }

    public async Task<CommandResult> StopManualAsync(
        string runtimeId,
        bool resumeNavigation,
        CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return CommandResult.Fail("Robot runtime was not found.");
        var result = await SendCommandAsync(runtime, new
        {
            command = "manual_stop",
            resume_navigation = resumeNavigation,
        }, cancellationToken);
        if (!result.Success) return result;
        if (!HasStm32Acknowledgement(result))
            return CommandResult.Fail("Robot gateway accepted the stop, but STM32 did not acknowledge it.");

        lock (runtime.SyncRoot)
        {
            if (result.Payload is JsonElement payload
                && payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("navigation_active", out var navigationActive)
                && (navigationActive.ValueKind == JsonValueKind.True || navigationActive.ValueKind == JsonValueKind.False))
            {
                runtime.NavigationActive = navigationActive.GetBoolean();
            }
        }
        return result;
    }

    private static bool HasStm32Acknowledgement(CommandResult result)
    {
        if (result.Payload is not JsonElement payload
            || payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("stm32_acknowledged", out var acknowledged))
        {
            return false;
        }

        return acknowledged.ValueKind == JsonValueKind.True;
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
        if (result.Success)
        {
            lock (runtime.SyncRoot)
            {
                runtime.MappingActive = enabled;
            }
        }
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

    public async Task<CommandResult> StartNavigationModeAsync(
        string runtimeId,
        Guid mapId,
        string mapPath,
        IReadOnlyList<NavigationZonePolicy> zones,
        string? leaseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetReadyRuntime(runtimeId, leaseId, requireLocalized: false, out var runtime, out var error))
            return CommandResult.Fail(error!);
        if (runtime!.MappingActive) return CommandResult.Fail("Stop SLAM mapping before starting Nav2.");
        var result = await SendCommandAsync(
            runtime,
            new
            {
                command = "start_navigation_mode",
                map_id = mapId.ToString("D"),
                map_path = mapPath,
                zones = zones.Select(zone => new
                {
                    zone_type = zone.ZoneType,
                    min_x = zone.MinX,
                    min_y = zone.MinY,
                    max_x = zone.MaxX,
                    max_y = zone.MaxY,
                    width = zone.Width,
                    height = zone.Height,
                    rotation_angle = zone.RotationAngle,
                    max_speed = zone.MaxSpeed,
                }),
            },
            cancellationToken,
            TimeSpan.FromSeconds(70));
        if (result.Success)
        {
            lock (runtime.SyncRoot)
            {
                runtime.NavigationActive = true;
            }
        }
        return result;
    }

    public async Task<CommandResult> UpdateNavigationZonesAsync(
        string runtimeId,
        Guid mapId,
        IReadOnlyList<NavigationZonePolicy> zones,
        CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime))
            return CommandResult.Fail("Robot runtime was not found.");

        var response = ToResponse(runtime);
        if (!response.Connected || response.Stale)
            return CommandResult.Fail("Robot telemetry is offline or stale.");
        if (!response.NavigationActive
            || !Guid.TryParse(response.Telemetry?.ActiveMapId, out var activeMapId)
            || activeMapId != mapId)
            return CommandResult.Fail("The requested map is not currently active in navigation.");

        return await SendCommandAsync(
            runtime,
            new
            {
                command = "update_navigation_zones",
                map_id = mapId.ToString("D"),
                zones = zones.Select(zone => new
                {
                    zone_type = zone.ZoneType,
                    min_x = zone.MinX,
                    min_y = zone.MinY,
                    max_x = zone.MaxX,
                    max_y = zone.MaxY,
                    width = zone.Width,
                    height = zone.Height,
                    rotation_angle = zone.RotationAngle,
                    max_speed = zone.MaxSpeed,
                }),
            },
            cancellationToken,
            TimeSpan.FromSeconds(20));
    }

    public async Task<CommandResult> StopNavigationModeAsync(string runtimeId, CancellationToken cancellationToken)
    {
        if (!_runtimes.TryGetValue(runtimeId, out var runtime)) return CommandResult.Fail("Robot runtime was not found.");
        var result = await SendCommandAsync(runtime, new { command = "stop_navigation_mode" }, cancellationToken);
        if (result.Success)
        {
            lock (runtime.SyncRoot)
            {
                runtime.NavigationActive = false;
            }
        }
        return result;
    }

    public async Task<CommandResult> SetInitialPoseAsync(
        string runtimeId,
        InitialPoseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetConnectedRuntime(runtimeId, out var runtime, out var error))
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
        if (runtime!.Telemetry?.NavigationReady != true)
            return CommandResult.Fail("Nav2 is not ready. Set the initial pose and wait for activation.");

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
                    await _eventPublisher.PublishConnectionChangedAsync(runtime.Endpoint.Id, ToResponse(runtime), stoppingToken);
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

    private bool TryGetConnectedRuntime(
        string runtimeId,
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
        error = null;
        return true;
    }

    private async Task<CommandResult> SendCommandAsync(
        RuntimeState runtime,
        object command,
        CancellationToken cancellationToken,
        TimeSpan? responseTimeout = null)
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
            timeout.CancelAfter(responseTimeout ?? TimeSpan.FromSeconds(10));
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
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public int MaxWheelSpeedMmS { get; set; }
        public string? DisplayName { get; set; }
        public string? Kind { get; set; }
    }
}

