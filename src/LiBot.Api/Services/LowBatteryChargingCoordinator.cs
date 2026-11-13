using System.Collections.Concurrent;
using System.Text.Json;
using LiBot.Api.Hubs;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Robot;
using LiBot.Application.Services.Robot;
using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace LiBot.Api.Services;

public sealed class LowBatteryChargingCoordinator(
    IServiceScopeFactory scopeFactory,
    IRobotRuntimeService runtimes,
    ILiBotTcpClient tcpClient,
    IHubContext<RobotHub> hub,
    IConfiguration configuration,
    ILogger<LowBatteryChargingCoordinator> logger) : BackgroundService
{
    private const double LowBatteryThreshold = 50;
    private const double RearmBatteryThreshold = 60;
    private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan AlertRepeatInterval = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, RuntimeEpisode> _episodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Channel<(string RuntimeId, RobotTelemetryRequest Telemetry)> _telemetryQueue =
        Channel.CreateUnbounded<(string RuntimeId, RobotTelemetryRequest Telemetry)>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public Task OnTelemetryUpdatedAsync(
        string runtimeId,
        RobotTelemetryRequest telemetry)
    {
        if (!string.Equals(runtimeId, "real", StringComparison.OrdinalIgnoreCase)
            || telemetry.Battery is not double battery
            || !double.IsFinite(battery)
            || battery is < 0 or > 100)
            return Task.CompletedTask;

        _telemetryQueue.Writer.TryWrite((runtimeId, telemetry));
        return Task.CompletedTask;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _telemetryQueue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessTelemetryAsync(item.RuntimeId, item.Telemetry, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Low-battery telemetry processing failed.");
            }
        }
    }

    private async Task ProcessTelemetryAsync(
        string runtimeId,
        RobotTelemetryRequest telemetry,
        CancellationToken cancellationToken)
    {
        var battery = telemetry.Battery!.Value;
        var episode = _episodes.GetOrAdd("real", _ => new RuntimeEpisode());
        await episode.Gate.WaitAsync(cancellationToken);
        try
        {
            var runtime = runtimes.Get("real");
            if (runtime?.Telemetry?.Battery is double latestBattery
                && double.IsFinite(latestBattery)
                && latestBattery is >= 0 and <= 100)
                battery = latestBattery;

            if (battery >= RearmBatteryThreshold)
            {
                episode.Triggered = false;
                episode.NextAttemptAt = DateTimeOffset.MinValue;
                episode.LastAlertReason = null;
                return;
            }
            if (battery > LowBatteryThreshold || episode.Triggered)
                return;

            var now = DateTimeOffset.UtcNow;
            if (now < episode.NextAttemptAt)
                return;

            var preconditionError = GetRuntimePreconditionError(runtime);
            if (preconditionError is not null)
            {
                await NotifyBlockedAsync(episode, battery, preconditionError, cancellationToken);
                return;
            }

            var activeMapText = runtime?.Telemetry?.ActiveMapId;
            if (!Guid.TryParse(activeMapText, out var mapId))
            {
                await NotifyBlockedAsync(episode, battery, "Robot telemetry does not identify its active map.", cancellationToken);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LiBotDbContext>();
            var workflows = scope.ServiceProvider.GetRequiredService<IWorkflowManagementService>();
            var missionData = scope.ServiceProvider.GetRequiredService<IMissionDataRepository>();

            var chargingWorkflowId = await db.Workflows.AsNoTracking()
                .Where(item => item.Type == "ROBOT_RETURN_AND_CHARGING" && item.Status == "Active")
                .Select(item => (Guid?)item.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (chargingWorkflowId is Guid existingWorkflowId)
            {
                var activeMission = await db.Jobs.AsNoTracking()
                    .Where(item => item.WorkflowId == existingWorkflowId && (item.Status == "Pending" || item.Status == "Running"))
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .Select(item => new { item.Id, item.Status })
                    .FirstOrDefaultAsync(cancellationToken);
                if (activeMission is not null)
                {
                    var existingTask = await missionData.GetTaskAsync(activeMission.Id, tracking: false, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(existingTask?.ExecutionId))
                    {
                        episode.Triggered = true;
                        await PublishAutoChargeStartedAsync(new
                        {
                            missionId = activeMission.Id,
                            executionId = existingTask.ExecutionId,
                            runtimeId = "real",
                            workflowId = existingWorkflowId,
                            batteryPercent = battery,
                            chargerEndpointId = (Guid?)null,
                            status = existingTask.Status.ToUpperInvariant(),
                            queued = existingTask.Status == "Pending"
                        }, cancellationToken);
                        return;
                    }

                    if (activeMission.Status == "Pending")
                    {
                        var abandonedMission = await missionData.GetMissionAsync(activeMission.Id, tracking: true, cancellationToken);
                        if (abandonedMission is not null) abandonedMission.Fail();
                        if (existingTask is not null)
                        {
                            var abandonedTask = await missionData.GetTaskAsync(activeMission.Id, tracking: true, cancellationToken);
                            abandonedTask?.UpdateExecutionState(JsonSerializer.Serialize(new
                            {
                                missionId = activeMission.Id,
                                runtimeId = "real",
                                status = "FAILED",
                                error = "Automatic charging dispatch was interrupted before a robot execution was recorded.",
                                updatedAt = DateTime.UtcNow
                            }));
                        }
                        await missionData.SaveChangesAsync(cancellationToken);
                    }
                    else
                    {
                        episode.Triggered = true;
                        await NotifyBlockedAsync(episode, battery,
                            "An active charging mission has no execution id. Automatic movement is paused to avoid creating a duplicate.",
                            cancellationToken);
                        return;
                    }
                }
            }

            var chargers = await db.Endpoints.AsNoTracking()
                .Where(item => item.MapId == mapId && item.IsActive && item.EndpointType == "CHARGER")
                .Select(item => new { item.Id, item.Name, item.PositionX, item.PositionY, item.Theta })
                .Take(2)
                .ToListAsync(cancellationToken);
            if (chargers.Count != 1)
            {
                var reason = chargers.Count == 0
                    ? "The active map has no active CHARGER endpoint. Add one before automatic charging can move the robot."
                    : "The active map has multiple CHARGER endpoints. Keep exactly one active charger endpoint for automatic charging.";
                await NotifyBlockedAsync(episode, battery, reason, cancellationToken);
                return;
            }
            var charger = chargers[0];

            var workflow = chargingWorkflowId is Guid workflowId
                ? await workflows.GetByIdAsync(workflowId, cancellationToken)
                : null;
            if (workflow is null)
            {
                var created = await workflows.CreateFromTemplateAsync(
                    "robot-return-charging",
                    new WorkflowTemplateCreateRequest("Robot Return & Charging"),
                    cancellationToken);
                if (created.Status != WorkflowOperationStatus.Success || created.Value is null)
                {
                    await NotifyBlockedAsync(episode, battery,
                        created.Message ?? "The automatic charging workflow could not be registered.", cancellationToken);
                    return;
                }
                workflow = created.Value;
            }

            JsonElement preflight;
            try
            {
                using var response = await tcpClient.SendAsync("real", new
                {
                    command = "preflight_workflow",
                    runtime_id = "real",
                    require_hardware = false,
                    allow_active_workflow = true
                }, cancellationToken);
                preflight = response.RootElement.Clone();
            }
            catch (LiBotTcpException exception)
            {
                await NotifyBlockedAsync(episode, battery, $"Robot preflight failed: {exception.Message}", cancellationToken);
                return;
            }
            if (!HasStatus(preflight, "ok"))
            {
                await NotifyBlockedAsync(episode, battery,
                    ReadMessage(preflight) ?? "Robot preflight failed. Automatic charging did not start.", cancellationToken);
                return;
            }
            var hasActiveWorkflow = preflight.TryGetProperty("checks", out var preflightChecks)
                && preflightChecks.ValueKind == JsonValueKind.Object
                && preflightChecks.TryGetProperty("active_workflow", out var activeWorkflowValue)
                && activeWorkflowValue.ValueKind == JsonValueKind.True;
            var navigationIsInProgress = runtime?.Telemetry is { } currentTelemetry
                && (!string.IsNullOrWhiteSpace(currentTelemetry.ActiveGoalId)
                    || currentTelemetry.NavigationState is "SENDING" or "NAVIGATING" or "CANCELING");
            if (navigationIsInProgress && !hasActiveWorkflow)
            {
                await NotifyBlockedAsync(episode, battery,
                    "A navigation goal is active outside a workflow mission. Automatic charging will wait until it finishes.",
                    cancellationToken);
                return;
            }

            runtime = runtimes.Get("real");
            preconditionError = GetRuntimePreconditionError(runtime);
            if (preconditionError is not null)
            {
                await NotifyBlockedAsync(episode, battery, preconditionError, cancellationToken);
                return;
            }
            if (!Guid.TryParse(runtime?.Telemetry?.ActiveMapId, out var currentMapId) || currentMapId != mapId)
            {
                await NotifyBlockedAsync(episode, battery, "The active map changed during preflight. Automatic charging will retry on the current map.", cancellationToken);
                return;
            }
            battery = runtime!.Telemetry!.Battery!.Value;
            if (battery >= RearmBatteryThreshold)
            {
                episode.Triggered = false;
                episode.NextAttemptAt = DateTimeOffset.MinValue;
                return;
            }
            if (battery > LowBatteryThreshold)
                return;

            var documentService = scope.ServiceProvider.GetRequiredService<IWorkflowDocumentService>();
            RobotWorkflowDocument document;
            try
            {
                document = await documentService.BuildRobotDocumentAsync(workflow.Id, "real", cancellationToken)
                    ?? throw new InvalidWorkflowDefinitionException("The automatic charging workflow is unavailable.");
            }
            catch (InvalidWorkflowDefinitionException exception)
            {
                await NotifyBlockedAsync(episode, battery, exception.Message, cancellationToken);
                return;
            }

            var robotId = await missionData.GetRobotIdByMapAsync(mapId, cancellationToken);
            if (robotId is null)
            {
                await NotifyBlockedAsync(episode, battery,
                    "No database robot is assigned to the active map, so the automatic charging mission cannot be recorded.",
                    cancellationToken);
                return;
            }

            var mission = new Mission(
                "Automatic return to charger",
                $"Battery reached {battery:0.##}%; return to CHARGER endpoint '{charger.Name}'.",
                robotId.Value,
                workflow.Id,
                priority: 4);

            var initialContext = new Dictionary<string, object?>
            {
                ["battery_pct"] = battery,
                ["charging_endpoint"] = new { x = charger.PositionX, y = charger.PositionY, theta = charger.Theta },
                ["charger_endpoint_id"] = charger.Id.ToString(),
                ["active_map_id"] = mapId.ToString(),
                ["runtime_id"] = "real",
                ["mission_id"] = mission.Id.ToString(),
                ["task_id"] = mission.Id.ToString(),
                ["backend_base_url"] = configuration["LiBot:BackendBaseUrl"]?.TrimEnd('/') ?? string.Empty
            };
            await missionData.CreateMissionAsync(
                mission,
                workflow.Id,
                "real",
                JsonSerializer.Serialize(initialContext),
                cancellationToken);

            var jobTask = await missionData.GetTaskAsync(mission.Id, tracking: true, cancellationToken)
                ?? throw new InvalidOperationException("The automatic charging task was not persisted.");
            var executionId = Guid.NewGuid().ToString("N");
            jobTask.SetExecutionId(executionId);
            jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
            {
                missionId = mission.Id,
                executionId,
                runtimeId = "real",
                status = "QUEUED",
                currentStep = (string?)null,
                currentStepType = (string?)null,
                expectedEventType = (string?)null,
                allowedValues = Array.Empty<string>(),
                updatedAt = DateTime.UtcNow
            }));
            await missionData.SaveChangesAsync(cancellationToken);

            JsonElement dispatchResponse;
            try
            {
                using var response = await tcpClient.SendAsync("real", new
                {
                    command = "start_auto_charge",
                    workflow_version = document.Version,
                    workflow_id = document.WorkflowId,
                    mission_id = mission.Id.ToString(),
                    task_id = mission.Id.ToString(),
                    execution_id = executionId,
                    runtime_id = "real",
                    initial_context = initialContext,
                    steps = document.Steps
                }, cancellationToken);
                dispatchResponse = response.RootElement.Clone();
            }
            catch (LiBotTcpException exception)
            {
                mission.Fail();
                jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
                {
                    missionId = mission.Id, executionId, runtimeId = "real", status = "FAILED",
                    error = exception.Message, updatedAt = DateTime.UtcNow
                }));
                await missionData.SaveChangesAsync(cancellationToken);
                await NotifyBlockedAsync(episode, battery, $"Robot rejected automatic charging: {exception.Message}", cancellationToken);
                return;
            }

            if (!HasStatus(dispatchResponse, "accepted"))
            {
                var message = ReadMessage(dispatchResponse) ?? "Robot rejected the automatic charging workflow.";
                mission.Fail();
                jobTask.UpdateExecutionState(JsonSerializer.Serialize(new
                {
                    missionId = mission.Id, executionId, runtimeId = "real", status = "FAILED",
                    error = message, updatedAt = DateTime.UtcNow
                }));
                await missionData.SaveChangesAsync(cancellationToken);
                await NotifyBlockedAsync(episode, battery, message, cancellationToken);
                return;
            }

            episode.Triggered = true;
            var queued = dispatchResponse.TryGetProperty("queued", out var queuedValue)
                && queuedValue.ValueKind == JsonValueKind.True;
            await PublishAutoChargeStartedAsync(new
            {
                missionId = mission.Id,
                executionId,
                runtimeId = "real",
                workflowId = workflow.Id,
                batteryPercent = battery,
                chargerEndpointId = charger.Id,
                status = queued ? "QUEUED" : "ACCEPTED",
                queued
            }, cancellationToken);
            await PublishLibrarianNotificationAsync(
                "LOW_BATTERY_RETURN_STARTED",
                $"Robot battery is {battery:0.#}%. It is {(queued ? "finishing its current safe step before returning" : "returning")} to charger '{charger.Name}'.",
                battery,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Automatic low-battery charging dispatch failed.");
            await NotifyBlockedAsync(episode, battery, "Automatic charging could not be started because of a backend error.", cancellationToken);
        }
        finally
        {
            episode.Gate.Release();
        }
    }

    private static string? GetRuntimePreconditionError(RobotRuntimeResponse? runtime)
    {
        if (runtime is null || !runtime.Connected || runtime.Stale)
            return "Robot runtime is offline or telemetry is stale. Automatic return is paused for safety.";
        if (runtime.Telemetry?.Battery is not double battery || !double.IsFinite(battery) || battery is < 0 or > 100)
            return "Battery telemetry is missing or invalid. Automatic charging is paused for safety.";
        if (!string.Equals(runtime.Telemetry?.LocalizationState, "LOCALIZED", StringComparison.OrdinalIgnoreCase))
            return "Robot is not localized. Set or restore localization before automatic charging.";
        if (runtime.Telemetry?.NavigationReady != true)
            return "Nav2 is not ready. Automatic charging will retry when navigation is ready.";
        return null;
    }

    private async Task NotifyBlockedAsync(
        RuntimeEpisode episode,
        double battery,
        string reason,
        CancellationToken cancellationToken)
    {
        episode.NextAttemptAt = DateTimeOffset.UtcNow.Add(RetryInterval);
        var now = DateTimeOffset.UtcNow;
        if (string.Equals(episode.LastAlertReason, reason, StringComparison.Ordinal)
            && now - episode.LastAlertAt < AlertRepeatInterval)
            return;

        episode.LastAlertReason = reason;
        episode.LastAlertAt = now;
        await PublishLibrarianNotificationAsync(
            "LOW_BATTERY_RETURN_BLOCKED",
            $"Robot battery is {battery:0.#}%, but it will not move to charge: {reason}",
            battery,
            cancellationToken);
    }

    private Task PublishAutoChargeStartedAsync(object payload, CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.LibrarianGroup).SendAsync("AutoChargeStarted", payload, cancellationToken);

    private Task PublishLibrarianNotificationAsync(
        string type,
        string message,
        double battery,
        CancellationToken cancellationToken) =>
        hub.Clients.Group(RobotHub.LibrarianGroup).SendAsync("LibrarianNotification", new
        {
            type,
            runtimeId = "real",
            batteryPercent = battery,
            message,
            createdAt = DateTime.UtcNow
        }, cancellationToken);

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

    private sealed class RuntimeEpisode
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public bool Triggered { get; set; }
        public DateTimeOffset NextAttemptAt { get; set; }
        public DateTimeOffset LastAlertAt { get; set; }
        public string? LastAlertReason { get; set; }
    }
}
