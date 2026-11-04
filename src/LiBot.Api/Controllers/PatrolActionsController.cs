using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflow-data/patrol/actions")]
public sealed class PatrolActionsController(
    IWorkflowDataService dataService,
    IRobotRuntimeService runtimes) : ControllerBase
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ActionLocks = new(StringComparer.Ordinal);
    private static readonly HashSet<string> SupportedActions = new(StringComparer.OrdinalIgnoreCase)
    {
        "PATROL_SCOPE",
        "BUILD_PATROL_COVERAGE_PLAN",
        "CLASSIFY_PATROL_OBSERVATIONS",
        "UPDATE_PATH_COVERAGE",
        "GENERATE_PATROL_REPORT"
    };

    [HttpPost]
    public async Task<IActionResult> Execute(PatrolActionRequest request, CancellationToken cancellationToken)
    {
        if (request.MissionId == Guid.Empty || string.IsNullOrWhiteSpace(request.RuntimeId)
            || string.IsNullOrWhiteSpace(request.Action) || request.Payload.ValueKind != JsonValueKind.Object
            || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            return BadRequest(new { message = "missionId, runtimeId, action, object payload and idempotencyKey are required." });

        var action = request.Action.Trim().ToUpperInvariant();
        if (!SupportedActions.Contains(action))
            return BadRequest(new { message = $"Unsupported patrol action '{action}'." });
        var runtimeId = request.RuntimeId.Trim().ToLowerInvariant();
        if (runtimeId is not ("real" or "sim"))
            return BadRequest(new { message = "runtimeId must be 'real' or 'sim'." });

        var mission = await dataService.GetMissionAsync(request.MissionId, cancellationToken);
        var activeSimulation = runtimeId == "sim"
            && SimulationExecutionStore.TryGetExecution(request.MissionId, out _, out var activeRuntime)
            && string.Equals(activeRuntime, "sim", StringComparison.OrdinalIgnoreCase);
        if (mission is null && !activeSimulation) return NotFound(new { message = "Mission task not found." });
        if (mission is not null && !string.Equals(mission.RuntimeId, runtimeId, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Runtime does not match the mission." });
        if (runtimeId == "sim" && !activeSimulation)
            return Conflict(new { message = "No active in-memory simulation exists for this mission." });

        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            action + ":" + request.Payload.GetRawText())));
        var cacheKey = $"{request.MissionId:N}:{request.IdempotencyKey.Trim()}";
        var gate = ActionLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var cached = await FindCachedActionAsync(request.MissionId, runtimeId, request.IdempotencyKey.Trim(), cancellationToken);
            if (cached is not null)
            {
                if (!string.Equals(cached.Fingerprint, fingerprint, StringComparison.Ordinal))
                    return Conflict(new { message = "idempotencyKey was already used with a different action or payload." });
                return Ok(cached.Response);
            }

            ActionResult result;
            try
            {
                result = await RunActionAsync(action, request, runtimeId, cancellationToken);
            }
            catch (PatrolActionException exception)
            {
                return StatusCode(exception.StatusCode, new { message = exception.Message });
            }

            var response = new
            {
                action_status = "SUCCEEDED",
                data = result.Data,
                request_id = Guid.NewGuid(),
                affected_records = result.AffectedRecords
            };
            var idempotencyRecord = JsonSerializer.Serialize(new
            {
                key = request.IdempotencyKey.Trim(),
                fingerprint,
                response
            });
            if (runtimeId == "sim")
                SimulationWorkflowDataStore.AddPatrolAction(request.MissionId, request.IdempotencyKey.Trim(), fingerprint, response);
            else
                await dataService.AddJobEventAsync(request.MissionId, "PATROL_ACTION_IDEMPOTENCY", idempotencyRecord, cancellationToken);
            return Ok(response);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<ActionResult> RunActionAsync(
        string action,
        PatrolActionRequest request,
        string runtimeId,
        CancellationToken cancellationToken) => action switch
    {
        "PATROL_SCOPE" => await ResolveScopeAsync(request, runtimeId, cancellationToken),
        "BUILD_PATROL_COVERAGE_PLAN" => await BuildCoveragePlanAsync(request, runtimeId, cancellationToken),
        "CLASSIFY_PATROL_OBSERVATIONS" => await ClassifyObservationsAsync(request, runtimeId, cancellationToken),
        "UPDATE_PATH_COVERAGE" => await UpdateCoverageAsync(request, runtimeId, cancellationToken),
        "GENERATE_PATROL_REPORT" => await GenerateReportAsync(request, runtimeId, cancellationToken),
        _ => throw new PatrolActionException(StatusCodes.Status400BadRequest, "Unsupported patrol action.")
    };

    private async Task<ActionResult> ResolveScopeAsync(
        PatrolActionRequest request, string runtimeId, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        var mapId = RequiredGuid(payload, "map_id");
        var map = await dataService.GetPatrolMapAsync(mapId, cancellationToken)
            ?? throw new PatrolActionException(StatusCodes.Status400BadRequest, "map_id does not identify a configured library map.");
        EnsureRuntimeMap(runtimeId, mapId);
        if (!string.Equals(map.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new PatrolActionException(StatusCodes.Status409Conflict, "Patrol map is not active.");

        var requestedFloorId = OptionalGuid(payload, "floor_id");
        if (requestedFloorId is not null && requestedFloorId != map.FloorId)
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "floor_id does not match the selected map.");
        var scopeType = ReadString(payload, "scope_type")?.Trim().ToUpperInvariant() ?? "ENDPOINTS";
        if (scopeType != "ENDPOINTS")
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "Only ENDPOINTS patrol scope is supported.");

        var endpointIds = ReadGuidArray(payload, "target_endpoints");
        if (endpointIds.Count == 0)
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "target_endpoints must contain at least one endpoint id.");
        if (endpointIds.Distinct().Count() != endpointIds.Count)
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "target_endpoints must not contain duplicates.");
        await ValidateEndpointsAsync(mapId, endpointIds, cancellationToken);

        return new ActionResult(new
        {
            map_id = mapId,
            floor_id = requestedFloorId ?? map.FloorId,
            scope_type = scopeType,
            target_endpoints = endpointIds
        }, 0);
    }

    private async Task<ActionResult> BuildCoveragePlanAsync(
        PatrolActionRequest request, string runtimeId, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        var mapId = RequiredGuid(payload, "map_id");
        EnsureRuntimeMap(runtimeId, mapId);
        var targetIds = ReadGuidArray(payload, "target_endpoints");
        if (targetIds.Count == 0)
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "target_endpoints must contain at least one endpoint id.");
        var endpointRecords = await ValidateEndpointsAsync(mapId, targetIds, cancellationToken);
        var endpointById = endpointRecords.ToDictionary(item => item.Id);

        List<JsonElement> remainingPaths;
        if (TryGet(payload, "remaining_paths", out var suppliedPaths) && suppliedPaths.ValueKind == JsonValueKind.Array)
        {
            remainingPaths = suppliedPaths.EnumerateArray().Select(item => item.Clone()).ToList();
        }
        else
        {
            var pose = TryGet(payload, "current_pose", out var poseValue) && poseValue.ValueKind == JsonValueKind.Object
                ? poseValue
                : runtimes.Get(runtimeId)?.Telemetry is { } telemetry
                    ? JsonSerializer.SerializeToElement(new { x = telemetry.Pose.X, y = telemetry.Pose.Y, theta = telemetry.Pose.Yaw })
                    : default;
            if (pose.ValueKind != JsonValueKind.Object)
                throw new PatrolActionException(StatusCodes.Status409Conflict, "Robot current pose is unavailable for patrol route planning.");
            var startX = RequiredFiniteNumber(pose, "x");
            var startY = RequiredFiniteNumber(pose, "y");
            var startTheta = OptionalFiniteNumber(pose, "theta") ?? OptionalFiniteNumber(pose, "yaw") ?? 0d;
            var route = new List<object>(targetIds.Count);
            Guid? previousEndpoint = null;
            var previousName = "Current robot pose";
            var previousX = startX;
            var previousY = startY;
            var previousTheta = startTheta;
            for (var index = 0; index < targetIds.Count; index++)
            {
                var endpoint = endpointById[targetIds[index]];
                var fromEndpoint = new
                {
                    endpoint_id = previousEndpoint,
                    name = previousName,
                    x = previousX,
                    y = previousY,
                    theta = previousTheta
                };
                var destination = new
                {
                    endpoint_id = endpoint.Id,
                    name = endpoint.Name,
                    x = endpoint.X,
                    y = endpoint.Y,
                    theta = endpoint.Theta
                };
                route.Add(new
                {
                    path_id = Guid.NewGuid(),
                    from_endpoint_id = previousEndpoint,
                    to_endpoint_id = endpoint.Id,
                    from_endpoint = fromEndpoint,
                    target = destination,
                    path_index = index + 1,
                    total_paths = targetIds.Count
                });
                previousEndpoint = endpoint.Id;
                previousName = endpoint.Name;
                previousX = endpoint.X;
                previousY = endpoint.Y;
                previousTheta = endpoint.Theta;
            }
            remainingPaths = route.Select(item => JsonSerializer.SerializeToElement(item)).ToList();
        }

        if (remainingPaths.Count == 0)
            throw new PatrolActionException(StatusCodes.Status409Conflict, "There are no remaining patrol paths to dispatch.");
        var currentPath = remainingPaths[0];
        var rest = remainingPaths.Skip(1).ToArray();
        var currentEndpoint = TryGet(currentPath, "from_endpoint", out var currentPathStart)
            ? currentPathStart.Clone()
            : JsonSerializer.SerializeToElement(new { });
        var nextEndpoint = TryGet(currentPath, "target", out var target) ? target.Clone() : JsonSerializer.SerializeToElement(new { });
        var totalPaths = TryGet(currentPath, "total_paths", out var total) && total.TryGetInt32(out var totalCount) ? totalCount : targetIds.Count;
        var data = new
        {
            current_endpoint = currentEndpoint,
            next_endpoint = nextEndpoint,
            current_path = currentPath,
            remaining_paths = rest,
            remaining_paths_count = rest.Length,
            total_paths = totalPaths
        };
        return new ActionResult(data, 0);
    }

    private async Task<ActionResult> ClassifyObservationsAsync(
        PatrolActionRequest request, string runtimeId, CancellationToken cancellationToken)
    {
        var pathId = RequiredString(request.Payload, "path_id");
        var executionId = ReadString(request.Payload, "execution_id");
        var observations = TryGet(request.Payload, "observations", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().ToArray()
            : Array.Empty<JsonElement>();
        var events = new List<object>(observations.Length);
        foreach (var observation in observations)
        {
            var category = ReadObservationCategory(observation);
            var item = new
            {
                event_id = Guid.NewGuid(),
                mission_id = request.MissionId,
                runtime_id = runtimeId,
                execution_id = executionId,
                path_id = pathId,
                category,
                severity = category == "FALLEN_BOOK" || category == "OBSTACLE" ? "HIGH" : "MEDIUM",
                observation,
                observed_at = DateTime.UtcNow
            };
            events.Add(item);
            if (runtimeId == "sim")
            {
                SimulationWorkflowDataStore.AddPatrolObservation(request.MissionId, item);
                SimulationWorkflowDataStore.AddPatrolCategory(request.MissionId, category);
            }
            else
            {
                await dataService.AddJobEventAsync(request.MissionId, "PATROL_OBSERVATION", JsonSerializer.Serialize(item), cancellationToken);
            }
        }

        var classificationData = JsonSerializer.Serialize(new { mission_id = request.MissionId, runtime_id = runtimeId, execution_id = executionId, path_id = pathId, events });
        var jobEventId = runtimeId == "sim"
            ? Guid.NewGuid()
            : await dataService.AddJobEventAsync(request.MissionId, "PATROL_OBSERVATIONS_CLASSIFIED", classificationData, cancellationToken);
        if (runtimeId == "sim")
            SimulationWorkflowDataStore.AddPatrolObservation(request.MissionId, new { event_type = "PATROL_OBSERVATIONS_CLASSIFIED", job_event_id = jobEventId, execution_id = executionId, path_id = pathId, events });
        return new ActionResult(new { events, job_event_id = jobEventId }, events.Count);
    }

    private async Task<ActionResult> UpdateCoverageAsync(
        PatrolActionRequest request, string runtimeId, CancellationToken cancellationToken)
    {
        var pathId = RequiredString(request.Payload, "path_id");
        var status = RequiredString(request.Payload, "status").ToUpperInvariant();
        if (status != "COVERED")
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "Path coverage status must be COVERED after a successful MOVE.");
        var jobEventId = RequiredGuid(request.Payload, "job_event_id");
        var executionId = ReadString(request.Payload, "execution_id");
        var actualDistance = OptionalFiniteNumber(request.Payload, "actual_distance_m") ?? 0d;
        if (actualDistance < 0)
            throw new PatrolActionException(StatusCodes.Status400BadRequest, "actual_distance_m cannot be negative.");
        var remaining = TryGet(request.Payload, "remaining_paths", out var remainingValue) && remainingValue.ValueKind == JsonValueKind.Array
            ? remainingValue.EnumerateArray().Select(item => item.Clone()).ToArray()
            : Array.Empty<JsonElement>();
        var planned = Math.Max(1, ReadInt(request.Payload, "planned_path_count") ?? 1);

        var priorCoverage = runtimeId == "sim"
            ? SimulationWorkflowDataStore.GetPatrolCoverage(request.MissionId)
            : (await dataService.GetJobEventDataAsync(request.MissionId, "PATROL_PATH_COVERAGE", cancellationToken))
                .Where(data => MatchesExecution(data, executionId)).Where(data => !string.IsNullOrWhiteSpace(data))
                .Select(data => ParseElement(data!)).Where(item => item.ValueKind == JsonValueKind.Object).ToArray();
        var priorCount = priorCoverage.Count(item => ReadString(item, "status") == "COVERED");
        var coveredAt = DateTime.UtcNow;
        var coverageEvent = new
        {
            mission_id = request.MissionId,
            runtime_id = runtimeId,
            execution_id = executionId,
            job_event_id = jobEventId,
            path_id = pathId,
            status,
            actual_distance_m = actualDistance,
            covered_at = coveredAt,
            planned_path_count = planned
        };
        Guid coverageEventId;
        if (runtimeId == "sim")
        {
            SimulationWorkflowDataStore.AddPatrolCoverage(request.MissionId, coverageEvent);
            coverageEventId = Guid.NewGuid();
        }
        else
        {
            coverageEventId = await dataService.AddJobEventAsync(request.MissionId, "PATROL_PATH_COVERAGE", JsonSerializer.Serialize(coverageEvent), cancellationToken);
        }
        var covered = priorCount + 1;
        return new ActionResult(new
        {
            coverage = new
            {
                coverage_event_id = coverageEventId,
                job_event_id = jobEventId,
                path_id = pathId,
                status,
                covered_paths = covered,
                total_paths = planned,
                coverage_percent = Math.Min(100d, 100d * covered / planned),
                actual_distance_m = actualDistance,
                covered_at = coveredAt
            },
            remaining_paths = remaining,
            remaining_paths_count = remaining.Length,
            total_paths = planned
        }, 1);
    }

    private async Task<ActionResult> GenerateReportAsync(
        PatrolActionRequest request, string runtimeId, CancellationToken cancellationToken)
    {
        var executionId = ReadString(request.Payload, "execution_id");
        var observationData = runtimeId == "sim"
            ? SimulationWorkflowDataStore.GetPatrolObservations(request.MissionId)
                .Where(item => ReadString(item, "event_type") is null)
                .ToArray()
            : (await dataService.GetJobEventDataAsync(request.MissionId, "PATROL_OBSERVATION", cancellationToken))
                .Where(data => MatchesExecution(data, executionId)).Where(data => !string.IsNullOrWhiteSpace(data))
                .Select(data => ParseElement(data!)).ToArray();
        var coverageData = runtimeId == "sim"
            ? SimulationWorkflowDataStore.GetPatrolCoverage(request.MissionId)
            : (await dataService.GetJobEventDataAsync(request.MissionId, "PATROL_PATH_COVERAGE", cancellationToken))
                .Where(data => MatchesExecution(data, executionId)).Where(data => !string.IsNullOrWhiteSpace(data))
                .Select(data => ParseElement(data!)).ToArray();

        var categories = observationData.Select(item => ReadString(item, "category") ?? "OTHER_WARNING").ToArray();
        var fallenBooks = categories.Count(item => item == "FALLEN_BOOK");
        var obstacles = categories.Count(item => item == "OBSTACLE");
        var warnings = categories.Count(item => item is "WARNING" or "OTHER_WARNING");
        var coveredCount = coverageData.Count(item => ReadString(item, "status") == "COVERED");
        var totalPaths = coverageData.Select(item => ReadInt(item, "planned_path_count") ?? 0).DefaultIfEmpty(0).Max();
        var started = (await dataService.GetMissionAsync(request.MissionId, cancellationToken))?.StartedAtUtc;
        var durationSeconds = started is DateTime startedAt ? Math.Max(0, (int)(DateTime.UtcNow - startedAt).TotalSeconds) : 0;
        var reportData = new
        {
            report_type = runtimeId == "sim" ? "SIMULATION_PATROL" : "PATROL",
            mission_id = request.MissionId,
            runtime_id = runtimeId,
            execution_id = executionId,
            duration_seconds = durationSeconds,
            observation_count = observationData.Length,
            fallen_book_count = fallenBooks,
            obstacle_count = obstacles,
            warning_count = warnings,
            path_coverage = new
            {
                covered_paths = coveredCount,
                total_paths = totalPaths,
                coverage_percent = totalPaths == 0 ? 0 : Math.Min(100d, 100d * coveredCount / totalPaths),
                paths = coverageData
            },
            observations = observationData
        };

        Guid reportId;
        if (runtimeId == "sim")
        {
            reportId = Guid.NewGuid();
            var simulationReport = new { report_id = reportId, reportData.report_type, reportData.mission_id, reportData.runtime_id, reportData.execution_id, reportData.duration_seconds, reportData.observation_count, reportData.fallen_book_count, reportData.obstacle_count, reportData.warning_count, reportData.path_coverage, reportData.observations };
            SimulationWorkflowDataStore.SetPatrolReport(request.MissionId, simulationReport);
            return new ActionResult(simulationReport, 1);
        }

        reportId = await dataService.SavePatrolReportAsync(request.MissionId, durationSeconds,
            observationData.Length, warnings + obstacles, JsonSerializer.Serialize(reportData), cancellationToken);
        var result = new { report_id = reportId, reportData.report_type, reportData.mission_id, reportData.runtime_id, reportData.execution_id, reportData.duration_seconds, reportData.observation_count, reportData.fallen_book_count, reportData.obstacle_count, reportData.warning_count, reportData.path_coverage, reportData.observations };
        return new ActionResult(result, 1);
    }

    private async Task<IReadOnlyList<PatrolEndpointRecord>> ValidateEndpointsAsync(
        Guid mapId, IReadOnlyCollection<Guid> endpointIds, CancellationToken cancellationToken)
    {
        var map = await dataService.GetPatrolMapAsync(mapId, cancellationToken)
            ?? throw new PatrolActionException(StatusCodes.Status400BadRequest, "map_id does not identify a configured library map.");
        if (!string.Equals(map.Status, "Active", StringComparison.OrdinalIgnoreCase))
            throw new PatrolActionException(StatusCodes.Status409Conflict, "Patrol map is not active.");
        var endpoints = await dataService.GetPatrolEndpointsAsync(endpointIds, cancellationToken);
        var byId = endpoints.ToDictionary(item => item.Id);
        foreach (var id in endpointIds)
        {
            if (!byId.TryGetValue(id, out var endpoint))
                throw new PatrolActionException(StatusCodes.Status400BadRequest, $"Endpoint '{id}' does not exist.");
            if (!endpoint.IsActive)
                throw new PatrolActionException(StatusCodes.Status409Conflict, $"Endpoint '{id}' is not active.");
            if (endpoint.MapId != mapId)
                throw new PatrolActionException(StatusCodes.Status400BadRequest, $"Endpoint '{id}' belongs to another map.");
        }
        return endpointIds.Select(id => byId[id]).ToArray();
    }

    private void EnsureRuntimeMap(string runtimeId, Guid mapId)
    {
        var runtime = runtimes.Get(runtimeId);
        if (runtime is null) throw new PatrolActionException(StatusCodes.Status400BadRequest, $"Robot runtime '{runtimeId}' is not configured.");
        var activeMap = runtime.Telemetry?.ActiveMapId;
        if (string.IsNullOrWhiteSpace(activeMap) || !Guid.TryParse(activeMap, out var activeMapId))
            throw new PatrolActionException(StatusCodes.Status409Conflict, "Robot runtime has no active map for patrol.");
        if (activeMapId != mapId)
            throw new PatrolActionException(StatusCodes.Status409Conflict, "map_id does not match the robot runtime's active map.");
    }

    private async Task<CachedAction?> FindCachedActionAsync(
        Guid missionId, string runtimeId, string key, CancellationToken cancellationToken)
    {
        if (runtimeId == "sim")
        {
            return SimulationWorkflowDataStore.TryGetPatrolAction(missionId, key, out var fingerprint, out var response)
                ? new CachedAction(fingerprint, response)
                : null;
        }
        var events = await dataService.GetJobEventDataAsync(missionId, "PATROL_ACTION_IDEMPOTENCY", cancellationToken);
        foreach (var data in events.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            var root = ParseElement(data!);
            if (!string.Equals(ReadString(root, "key"), key, StringComparison.Ordinal)) continue;
            if (!TryGet(root, "response", out var response) || response.ValueKind != JsonValueKind.Object) continue;
            return new CachedAction(ReadString(root, "fingerprint") ?? string.Empty, response.Clone());
        }
        return null;
    }

    private static bool MatchesExecution(string? data, string? executionId)
    {
        if (string.IsNullOrWhiteSpace(data)) return false;
        var element = ParseElement(data);
        return string.Equals(ReadString(element, "execution_id"), executionId, StringComparison.Ordinal);
    }

    private static JsonElement ParseElement(string data)
    {
        using var document = JsonDocument.Parse(data);
        return document.RootElement.Clone();
    }

    private static string ReadObservationCategory(JsonElement observation)
    {
        var candidate = observation.ValueKind == JsonValueKind.Object
            ? TryGet(observation, "category", out var category) ? category
                : TryGet(observation, "type", out var type) ? type : default
            : observation;
        var normalized = candidate.ValueKind == JsonValueKind.String
            ? candidate.GetString()?.Trim().Replace(' ', '_').Replace('-', '_').ToUpperInvariant()
            : "OTHER_WARNING";
        return normalized switch
        {
            "FALLEN_BOOK" or "BOOK_FALLEN" => "FALLEN_BOOK",
            "OBSTACLE" or "UNKNOWN_OBSTACLE" => "OBSTACLE",
            "WARNING" => "WARNING",
            _ => "OTHER_WARNING"
        };
    }

    private static List<Guid> ReadGuidArray(JsonElement element, string name)
    {
        if (!TryGet(element, name, out var value) || value.ValueKind != JsonValueKind.Array) return [];
        var result = new List<Guid>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var id))
                throw new PatrolActionException(StatusCodes.Status400BadRequest, $"{name} must contain only GUIDs.");
            result.Add(id);
        }
        return result;
    }

    private static Guid RequiredGuid(JsonElement element, string name) =>
        OptionalGuid(element, name) is Guid id && id != Guid.Empty
            ? id
            : throw new PatrolActionException(StatusCodes.Status400BadRequest, $"{name} must be a valid GUID.");

    private static Guid? OptionalGuid(JsonElement element, string name) =>
        TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var id)
            ? id : null;

    private static string RequiredString(JsonElement element, string name) =>
        ReadString(element, name) is { Length: > 0 } value
            ? value
            : throw new PatrolActionException(StatusCodes.Status400BadRequest, $"{name} is required.");

    private static string? ReadString(JsonElement element, string name) =>
        TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool TryGet(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
        }
        value = default;
        return false;
    }

    private static double RequiredFiniteNumber(JsonElement element, string name) =>
        OptionalFiniteNumber(element, name) is double value
            ? value
            : throw new PatrolActionException(StatusCodes.Status400BadRequest, $"{name} must be a finite number.");

    private static double? OptionalFiniteNumber(JsonElement element, string name) =>
        TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out var number) && double.IsFinite(number) ? number : null;

    private static int? ReadInt(JsonElement element, string name) =>
        TryGet(element, name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number : null;

    private sealed record ActionResult(object Data, int AffectedRecords);
    private sealed record CachedAction(string Fingerprint, JsonElement Response);
    private sealed class PatrolActionException(int statusCode, string message) : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
    }
}
