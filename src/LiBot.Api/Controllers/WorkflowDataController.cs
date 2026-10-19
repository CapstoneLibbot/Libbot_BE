using System.Text.Json;
using LiBot.Domain.Library;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflow-data")]
public sealed class WorkflowDataController(LiBotDbContext db) : ControllerBase
{
    private static readonly Guid SimulationBookId = Guid.Parse("26000000-0000-0000-0000-000000000001");
    private static readonly Guid SimulationBookItemId = Guid.Parse("26000000-0000-0000-0000-000000000002");
    private static readonly Guid SimulationFacilityId = Guid.Parse("26000000-0000-0000-0000-000000000003");
    private static readonly IReadOnlyDictionary<string, string> SimulationBarcodes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BOOK-123"] = "A3",
            ["BOOK-456"] = "B1"
        };

    [HttpGet("simulation/books/search")]
    public IActionResult SearchSimulationBooks([FromQuery] string? query)
    {
        var fixture = new { bookId = SimulationBookId, isbn = "SIM-9780000000001", title = "Robotics and Library Technology", author = "LiBot Simulation", classification = "Simulation fixture", availableCopies = 1, image = (string?)null };
        var term = query?.Trim();
        var books = string.IsNullOrEmpty(term) || fixture.title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || fixture.author.Contains(term, StringComparison.OrdinalIgnoreCase)
            ? new[] { fixture } : Array.Empty<object>();
        return Ok(new { books, page = 1, pageSize = 20, totalCount = books.Length, totalPages = books.Length });
    }

    [HttpGet("simulation/books/{bookId:guid}")]
    public async Task<IActionResult> GetSimulationBook(Guid bookId, CancellationToken cancellationToken)
    {
        if (bookId != SimulationBookId) return NotFound();
        var endpoint = await GetSimulationEndpointAsync(cancellationToken);
        if (endpoint is null) return Conflict(new { message = "Simulation shelf endpoint A3 is not configured." });
        return Ok(new
        {
            bookId = SimulationBookId,
            isbn = "SIM-9780000000001",
            title = "Robotics and Library Technology",
            author = "LiBot Simulation",
            classification = "Simulation fixture",
            actions = new[] { "VIEW_LOCATION", "START_GUIDANCE" },
            bookItems = new[] { new
            {
                id = SimulationBookItemId,
                barcode = "BOOK-123",
                status = "Available",
                location = new
                {
                    floor = "Simulation floor",
                    aisle = "A",
                    shelf = "A3",
                    shelfLevel = 2,
                    endpoint = new { endpointId = endpoint.Id, endpoint.Name, x = endpoint.PositionX, y = endpoint.PositionY, theta = endpoint.Theta }
                }
            } }
        });
    }

    [HttpGet("simulation/facilities/search")]
    public async Task<IActionResult> SearchSimulationFacilities([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var endpoint = await GetSimulationEndpointAsync(cancellationToken);
        if (endpoint is null) return Conflict(new { message = "Simulation point of interest is not configured." });
        var mapName = await db.Maps.AsNoTracking().Where(item => item.Id == endpoint.MapId)
            .Select(item => item.Name).FirstOrDefaultAsync(cancellationToken) ?? "Simulation map";
        var fixture = new { facilityId = SimulationFacilityId, name = "Information desk (simulation)", type = "SERVICE", mapId = endpoint.MapId, mapName };
        var term = query?.Trim();
        var facilities = string.IsNullOrEmpty(term) || fixture.name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || "information help desk service".Contains(term, StringComparison.OrdinalIgnoreCase)
            ? new[] { fixture } : Array.Empty<object>();
        // Keep the simulation usable when an admin enters a search term outside the fixture vocabulary.
        if (facilities.Length == 0) facilities = [fixture];
        return Ok(new { facilities });
    }

    [HttpGet("simulation/facilities/{facilityId:guid}")]
    public async Task<IActionResult> GetSimulationFacility(Guid facilityId, CancellationToken cancellationToken)
    {
        if (facilityId != SimulationFacilityId) return NotFound();
        var endpoint = await GetSimulationEndpointAsync(cancellationToken);
        if (endpoint is null) return Conflict(new { message = "Simulation point of interest is not configured." });
        var map = await db.Maps.AsNoTracking().FirstOrDefaultAsync(item => item.Id == endpoint.MapId, cancellationToken);
        if (map is null) return Conflict(new { message = "Simulation map is not configured." });
        return Ok(new
        {
            facilityId = SimulationFacilityId,
            name = "Information desk (simulation)",
            type = "SERVICE",
            description = "Fixture used only by the Gazebo runtime.",
            actions = new[] { "VIEW_LOCATION" },
            location = new
            {
                mapId = map.Id,
                mapName = map.Name,
                mapWidth = map.Width,
                mapHeight = map.Height,
                endpoint = new { endpointId = endpoint.Id, endpoint.Name, x = endpoint.PositionX, y = endpoint.PositionY, theta = endpoint.Theta }
            }
        });
    }

    [HttpPost("inventory/scan")]
    public async Task<IActionResult> RecordInventoryScan(InventoryScanRequest request, CancellationToken cancellationToken)
    {
        if (request.MissionId == Guid.Empty || string.IsNullOrWhiteSpace(request.Barcode))
            return BadRequest(new { message = "missionId and barcode are required." });
        var task = await db.JobTasks.FirstOrDefaultAsync(item => item.JobId == request.MissionId, cancellationToken);
        if (task is null) return NotFound(new { message = "Mission task not found." });
        if (!MatchesRuntime(task.RuntimeId, request.RuntimeId)) return Conflict(new { message = "Runtime does not match the mission." });

        var barcode = request.Barcode.Trim();
        if (task.RuntimeId == "real" && string.IsNullOrWhiteSpace(request.ShelfCode))
            return BadRequest(new { message = "shelfCode is required for a physical inventory scan." });
        var shelfLevelId = task.RuntimeId == "sim" ? null : await ResolveShelfLevelAsync(request.ShelfCode, cancellationToken);
        if (task.RuntimeId == "real" && shelfLevelId is null)
            return BadRequest(new { message = $"No active shelf level was found for shelf '{request.ShelfCode}'." });
        var item = task.RuntimeId == "sim" ? null : await db.BookItems.FirstOrDefaultAsync(bookItem => bookItem.Barcode == barcode, cancellationToken);
        string? fixtureShelf = null;
        var fixtureKnown = task.RuntimeId == "sim" && SimulationBarcodes.TryGetValue(barcode, out fixtureShelf);
        var known = task.RuntimeId == "sim" ? fixtureKnown : item is not null;
        var expectedShelfLevelId = item?.ExpectedShelfLevelId;
        var expectedShelfCode = task.RuntimeId == "sim" ? (fixtureKnown ? fixtureShelf : null) : null;
        var misplaced = task.RuntimeId == "sim"
            ? fixtureKnown && !string.Equals(fixtureShelf, request.ShelfCode, StringComparison.OrdinalIgnoreCase)
            : known && shelfLevelId is Guid scannedShelf && expectedShelfLevelId is Guid expected && scannedShelf != expected;
        if (expectedShelfLevelId is Guid expectedId)
        {
            expectedShelfCode = await (from level in db.ShelfLevels
                                       join shelf in db.Shelves on level.ShelfId equals shelf.Id
                                       where level.Id == expectedId
                                       select shelf.Code).FirstOrDefaultAsync(cancellationToken);
        }

        if (task.RuntimeId == "real" && item is not null)
        {
            item.RecordInventory(shelfLevelId);
            await db.SaveChangesAsync(cancellationToken);
        }

        var scan = new
        {
            barcode,
            shelfCode = request.ShelfCode,
            shelfLevelId,
            known,
            misplaced,
            expectedShelfCode,
            runtimeId = task.RuntimeId,
            scannedAt = DateTime.UtcNow
        };
        db.JobEvents.Add(new JobEvent(request.MissionId, "INVENTORY_SCAN", JsonSerializer.Serialize(scan)));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(scan);
    }

    [HttpPost("inventory/reports")]
    public async Task<IActionResult> FinalizeInventory(InventoryFinalizeRequest request, CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.FirstOrDefaultAsync(item => item.JobId == request.MissionId, cancellationToken);
        if (task is null) return NotFound(new { message = "Mission task not found." });
        if (!MatchesRuntime(task.RuntimeId, request.RuntimeId)) return Conflict(new { message = "Runtime does not match the mission." });

        var events = await db.JobEvents.AsNoTracking()
            .Where(item => item.JobId == request.MissionId && item.EventType == "INVENTORY_SCAN")
            .Select(item => item.Data)
            .ToListAsync(cancellationToken);
        var scannedBarcodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var scannedShelfLevels = new HashSet<Guid>();
        var misplacedCount = 0;
        foreach (var data in events)
        {
            if (string.IsNullOrWhiteSpace(data)) continue;
            using var document = JsonDocument.Parse(data);
            var root = document.RootElement;
            if (root.TryGetProperty("barcode", out var barcode) && barcode.ValueKind == JsonValueKind.String)
                scannedBarcodes.Add(barcode.GetString()!);
            if (root.TryGetProperty("shelfLevelId", out var shelfLevel) && shelfLevel.ValueKind == JsonValueKind.String
                && Guid.TryParse(shelfLevel.GetString(), out var levelId)) scannedShelfLevels.Add(levelId);
            if (root.TryGetProperty("misplaced", out var misplaced) && misplaced.ValueKind == JsonValueKind.True)
                misplacedCount++;
        }

        var missingCount = 0;
        if (task.RuntimeId == "real" && scannedShelfLevels.Count > 0)
        {
            var shelfLevelIds = scannedShelfLevels.ToArray();
            var barcodeArray = scannedBarcodes.ToArray();
            missingCount = await db.BookItems.CountAsync(item =>
                item.ExpectedShelfLevelId.HasValue
                && shelfLevelIds.Contains(item.ExpectedShelfLevelId.Value)
                && !barcodeArray.Contains(item.Barcode), cancellationToken);
        }

        var reportType = string.IsNullOrWhiteSpace(request.ReportType) ? "INVENTORY" : request.ReportType.Trim().ToUpperInvariant();
        var storedReportType = task.RuntimeId == "sim" ? $"SIMULATION_{reportType}" : reportType;
        var report = new Report(storedReportType,
            $"{reportType} mission {request.MissionId}: {scannedBarcodes.Count} scanned, {misplacedCount} misplaced, {missingCount} missing.");
        db.Reports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        db.InventoryReports.Add(new InventoryReport(report.Id, request.ScopeType ?? "SHELF", 1,
            scannedBarcodes.Count, Math.Max(0, scannedBarcodes.Count - misplacedCount), misplacedCount, missingCount));
        db.JobEvents.Add(new JobEvent(request.MissionId, "INVENTORY_REPORT", JsonSerializer.Serialize(new
        {
            reportId = report.Id,
            reportType = storedReportType,
            runtimeId = task.RuntimeId,
            totalBooksScanned = scannedBarcodes.Count,
            totalMisplaced = misplacedCount,
            totalMissing = missingCount
        })));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { reportId = report.Id, reportType = storedReportType, runtimeId = task.RuntimeId,
            totalBooksScanned = scannedBarcodes.Count, totalMisplaced = misplacedCount, totalMissing = missingCount });
    }

    [HttpPost("patrol/observations")]
    public async Task<IActionResult> RecordPatrolObservation(PatrolObservationRequest request, CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.FirstOrDefaultAsync(item => item.JobId == request.MissionId, cancellationToken);
        if (task is null) return NotFound(new { message = "Mission task not found." });
        if (!MatchesRuntime(task.RuntimeId, request.RuntimeId)) return Conflict(new { message = "Runtime does not match the mission." });
        if (request.Observation.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return BadRequest(new { message = "observation is required." });
        var category = ReadObservationCategory(request.Observation);
        var data = JsonSerializer.Serialize(new { category, observation = request.Observation, runtimeId = task.RuntimeId, observedAt = DateTime.UtcNow });
        db.JobEvents.Add(new JobEvent(request.MissionId, "PATROL_OBSERVATION", data));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { category, runtimeId = task.RuntimeId });
    }

    [HttpPost("patrol/reports")]
    public async Task<IActionResult> FinalizePatrol(PatrolFinalizeRequest request, CancellationToken cancellationToken)
    {
        var task = await db.JobTasks.FirstOrDefaultAsync(item => item.JobId == request.MissionId, cancellationToken);
        if (task is null) return NotFound(new { message = "Mission task not found." });
        if (!MatchesRuntime(task.RuntimeId, request.RuntimeId)) return Conflict(new { message = "Runtime does not match the mission." });
        var observationData = await db.JobEvents.AsNoTracking()
            .Where(item => item.JobId == request.MissionId && item.EventType == "PATROL_OBSERVATION")
            .Select(item => item.Data)
            .ToListAsync(cancellationToken);
        var categories = observationData.Select(ReadStoredObservationCategory).Where(item => item is not null).ToList();
        var fallen = categories.Count(item => item == "FALLEN_BOOK");
        var obstacles = categories.Count(item => item == "OBSTACLE");
        var warnings = categories.Count(item => item is "WARNING" or "OTHER_WARNING");
        var storedType = task.RuntimeId == "sim" ? "SIMULATION_PATROL" : "PATROL";
        var report = new Report(storedType,
            $"Patrol mission {request.MissionId}: {categories.Count} observations, {fallen} fallen books, {obstacles} obstacles, {warnings} warnings.");
        db.Reports.Add(report);
        await db.SaveChangesAsync(cancellationToken);
        var duration = task.StartedAtUtc.HasValue
            ? Math.Max(0, (int)(DateTime.UtcNow - task.StartedAtUtc.Value).TotalSeconds)
            : 0;
        db.PatrolReports.Add(new PatrolReport(report.Id, duration, categories.Count, warnings + fallen + obstacles,
            fallen, obstacles, warnings, "COMPLETED"));
        db.JobEvents.Add(new JobEvent(request.MissionId, "PATROL_REPORT", JsonSerializer.Serialize(new
        {
            reportId = report.Id,
            reportType = storedType,
            runtimeId = task.RuntimeId,
            observationCount = categories.Count,
            fallenBookCount = fallen,
            unknownObstacleCount = obstacles,
            otherWarningCount = warnings
        })));
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { reportId = report.Id, reportType = storedType, runtimeId = task.RuntimeId,
            observationCount = categories.Count, fallenBookCount = fallen, obstacleCount = obstacles, warningCount = warnings });
    }

    private async Task<LibraryEndpoint?> GetSimulationEndpointAsync(CancellationToken cancellationToken) =>
        await db.Endpoints.AsNoTracking().FirstOrDefaultAsync(item =>
            item.IsActive && EF.Functions.Like(item.Name, "%A3%"), cancellationToken);

    private async Task<Guid?> ResolveShelfLevelAsync(string? shelfCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(shelfCode)) return null;
        return await (from level in db.ShelfLevels
                      join shelf in db.Shelves on level.ShelfId equals shelf.Id
                      where shelf.Code == shelfCode
                      orderby level.LevelNumber
                      select (Guid?)level.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private static bool MatchesRuntime(string assignedRuntime, string requestedRuntime) =>
        string.Equals(assignedRuntime, requestedRuntime, StringComparison.OrdinalIgnoreCase);

    private static string ReadObservationCategory(JsonElement observation)
    {
        var candidate = observation.ValueKind == JsonValueKind.Object
            ? observation.TryGetProperty("category", out var category) ? category
                : observation.TryGetProperty("type", out var type) ? type : default
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

    private static string? ReadStoredObservationCategory(string? data)
    {
        if (string.IsNullOrWhiteSpace(data)) return null;
        try
        {
            using var document = JsonDocument.Parse(data);
            return document.RootElement.TryGetProperty("category", out var category) ? category.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}

public sealed record InventoryScanRequest(Guid MissionId, string RuntimeId, string Barcode, string? ShelfCode = null);
public sealed record InventoryFinalizeRequest(Guid MissionId, string RuntimeId, string? ShelfCode = null,
    string? ScopeType = null, string? ReportType = null);
public sealed record PatrolObservationRequest(Guid MissionId, string RuntimeId, JsonElement Observation);
public sealed record PatrolFinalizeRequest(Guid MissionId, string RuntimeId);
