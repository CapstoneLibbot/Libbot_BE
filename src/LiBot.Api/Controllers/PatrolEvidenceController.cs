using System.Collections.Concurrent;
using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflow-data/patrol/evidence")]
public sealed class PatrolEvidenceController(
    IWorkflowDataService dataService,
    IPatrolNotificationPublisher notifications,
    IConfiguration configuration,
    IWebHostEnvironment environment,
    ILogger<PatrolEvidenceController> logger) : ControllerBase
{
    private const int MaxImageBytes = 8 * 1024 * 1024;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> MissionLocks = new();

    [HttpPost]
    [RequestSizeLimit(12 * 1024 * 1024)]
    public async Task<IActionResult> Save([FromBody] PatrolEvidenceRequest request, CancellationToken cancellationToken)
    {
        if (request.MissionId == Guid.Empty || request.ObservationId == Guid.Empty || request.PathId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.ExecutionId)
            || !string.Equals(request.RuntimeId, "real", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "missionId, executionId, pathId, observationId and runtimeId=real are required." });
        if (!double.IsFinite(request.Confidence) || request.Confidence is < 0.70 or > 1.0)
            return BadRequest(new { message = "confidence must be between 0.70 and 1.0." });
        if (request.Waypoint.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
            return BadRequest(new { message = "waypoint must be an object or null." });

        byte[] image;
        try { image = Convert.FromBase64String(request.ImageBase64 ?? string.Empty); }
        catch (FormatException) { return BadRequest(new { message = "imageBase64 must contain a valid JPEG image." }); }
        if (image.Length < 4 || image.Length > MaxImageBytes
            || image[0] != 0xFF || image[1] != 0xD8 || image[2] != 0xFF)
            return BadRequest(new { message = "Evidence must be a JPEG image no larger than 8 MB." });

        var mission = await dataService.GetMissionAsync(request.MissionId, cancellationToken);
        if (mission is null) return NotFound(new { message = "Mission task not found." });
        if (!string.Equals(mission.RuntimeId, "real", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Patrol evidence can only be attached to a real-runtime mission." });

        var gate = MissionLocks.GetOrAdd(request.MissionId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var events = await dataService.GetJobEventDataAsync(request.MissionId, "PATROL_OBSERVATION", cancellationToken);
            foreach (var existing in events.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                JsonDocument document;
                try { document = JsonDocument.Parse(existing!); }
                catch (JsonException) { continue; }
                using (document)
                {
                    var root = document.RootElement;
                    if (!string.Equals(ReadString(root, "observation_id"), request.ObservationId.ToString(), StringComparison.OrdinalIgnoreCase)) continue;
                    var priorEvidenceId = Guid.TryParse(ReadString(root, "evidence_id"), out var parsed) ? parsed : request.ObservationId;
                    return Ok(new { observationId = request.ObservationId, evidenceId = priorEvidenceId,
                        imageUrl = EvidenceUrl(request.MissionId, priorEvidenceId), duplicate = true });
                }
            }

            var evidenceId = request.ObservationId;
            var imagePath = GetImagePath(request.MissionId, evidenceId);
            Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);
            await System.IO.File.WriteAllBytesAsync(imagePath, image, cancellationToken);

            var observedAt = request.ObservedAtUtc == default ? DateTime.UtcNow : request.ObservedAtUtc.ToUniversalTime();
            var waypoint = request.Waypoint.ValueKind == JsonValueKind.Object
                ? (object)request.Waypoint.Clone()
                : null!;
            var imageUrl = EvidenceUrl(request.MissionId, evidenceId);
            var eventData = JsonSerializer.Serialize(new
            {
                event_id = Guid.NewGuid(),
                mission_id = request.MissionId,
                runtime_id = "real",
                execution_id = request.ExecutionId,
                path_id = request.PathId.ToString(),
                category = "LEANING_BOOK_PRESENT",
                severity = "MEDIUM",
                observation_id = request.ObservationId,
                evidence_id = evidenceId,
                image_url = imageUrl,
                confidence = request.Confidence,
                waypoint,
                observed_at = observedAt
            });
            await dataService.AddJobEventAsync(request.MissionId, "PATROL_OBSERVATION", eventData, cancellationToken);

            try
            {
                await notifications.PublishLeaningBookDetectedAsync(new LeaningBookDetectedNotification(
                    request.MissionId, request.ExecutionId, request.PathId.ToString(), evidenceId,
                    imageUrl, request.Confidence, waypoint, observedAt), cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not publish the patrol leaning-book alert for mission {MissionId}.", request.MissionId);
            }

            return Ok(new { observationId = request.ObservationId, evidenceId, imageUrl, duplicate = false });
        }
        finally
        {
            gate.Release();
        }
    }

    [HttpGet("{missionId:guid}/{evidenceId:guid}")]
    public async Task<IActionResult> Get(Guid missionId, Guid evidenceId, CancellationToken cancellationToken)
    {
        var events = await dataService.GetJobEventDataAsync(missionId, "PATROL_OBSERVATION", cancellationToken);
        var belongsToMission = false;
        foreach (var value in events.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            try
            {
                using var document = JsonDocument.Parse(value!);
                if (Guid.TryParse(ReadString(document.RootElement, "evidence_id"), out var id) && id == evidenceId)
                {
                    belongsToMission = true;
                    break;
                }
            }
            catch (JsonException) { }
        }
        if (!belongsToMission) return NotFound();
        var imagePath = GetImagePath(missionId, evidenceId);
        if (!System.IO.File.Exists(imagePath)) return NotFound();
        return PhysicalFile(imagePath, "image/jpeg", enableRangeProcessing: true);
    }

    private string GetImagePath(Guid missionId, Guid evidenceId) =>
        Path.Combine(EvidenceRoot(), missionId.ToString("N"), $"{evidenceId:N}.jpg");

    private string EvidenceRoot()
    {
        var configured = configuration["PatrolEvidence:RootPath"];
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "patrol-evidence")
            : Path.GetFullPath(configured, environment.ContentRootPath);
    }

    private string EvidenceUrl(Guid missionId, Guid evidenceId) =>
        $"/api/workflow-data/patrol/evidence/{missionId:D}/{evidenceId:D}";

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

public sealed class PatrolEvidenceRequest
{
    public Guid MissionId { get; init; }
    public string RuntimeId { get; init; } = string.Empty;
    public string ExecutionId { get; init; } = string.Empty;
    public Guid ObservationId { get; init; }
    public Guid PathId { get; init; }
    public double Confidence { get; init; }
    public DateTime ObservedAtUtc { get; init; }
    public JsonElement Waypoint { get; init; }
    public string? ImageBase64 { get; init; }
}
