using System.Text.Json;
using LiBot.Application.Models;
using LiBot.Application.Interfaces;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Infrastructure.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Dữ liệu hỗ trợ workflow, gồm fixture Gazebo, kiểm kê sách và báo cáo tuần tra.</summary>
/// <remarks>Kết quả của runtime SIM được giữ tạm thời; dữ liệu runtime REAL được ghi vào database.</remarks>
[ApiController]
[Route("api/workflow-data")]
public sealed class WorkflowDataController(
    IWorkflowDataService dataService,
    IBookGuidanceService bookGuidance,
    IPatrolNotificationPublisher patrolNotifications) : ControllerBase
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

    /// <summary>Tìm sách trong bộ dữ liệu fixture dành riêng cho Gazebo.</summary>
    [HttpGet("simulation/books/search")]
    public async Task<IActionResult> SearchSimulationBooks([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var fixture = new { bookId = SimulationBookId, isbn = "SIM-9780000000001", title = "Robotics and Library Technology", author = "LiBot Simulation", classification = "Simulation fixture", availableCopies = 1, image = (string?)null };
        var term = query?.Trim();
        var catalog = await bookGuidance.SearchAsync(query, null, "FUZZY", 1, 20, cancellationToken);
        var books = catalog.Books
            .Where(book => book.AvailableCopies > 0)
            .Cast<object>()
            .ToList();
        var fixtureMatches = string.IsNullOrEmpty(term)
            || fixture.title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || fixture.author.Contains(term, StringComparison.OrdinalIgnoreCase);
        if (fixtureMatches) books.Add(fixture);
        return Ok(new
        {
            books,
            page = 1,
            pageSize = books.Count,
            totalCount = books.Count,
            totalPages = books.Count == 0 ? 0 : 1
        });
    }

    /// <summary>Lấy chi tiết sách mô phỏng và gắn vị trí kệ trong map Gazebo.</summary>
    [HttpGet("simulation/books/{bookId:guid}")]
    public async Task<IActionResult> GetSimulationBook(Guid bookId, CancellationToken cancellationToken)
    {
        var endpoint = await dataService.GetSimulationEndpointAsync(cancellationToken);
        if (endpoint is null) return Conflict(new { message = "Simulation shelf endpoint A3 is not configured." });

        if (bookId != SimulationBookId)
        {
            var book = await bookGuidance.GetBookAsync(bookId, cancellationToken);
            if (book is null) return NotFound();

            var simulationEndpoint = new NavigationEndpoint(
                endpoint.Id,
                endpoint.MapId,
                endpoint.Name,
                endpoint.X,
                endpoint.Y,
                Math.PI);
            var simulationItems = book.BookItems.Select(item =>
            {
                var location = item.Location;
                var simulationLocation = location is null
                    ? new BookLocation(
                        Guid.Empty, "Simulation floor", 1, Guid.Empty, "A", "Aisle A",
                        Guid.Empty, "A3", "Technology shelf A3", Guid.Empty, 2, simulationEndpoint)
                    : location with
                    {
                        Floor = "Simulation floor",
                        FloorNumber = 1,
                        Aisle = "A",
                        AisleName = "Aisle A",
                        Shelf = "A3",
                        ShelfName = "Technology shelf A3",
                        ShelfLevel = 2,
                        Endpoint = simulationEndpoint
                    };
                return item with { Location = simulationLocation };
            }).ToList();

            return Ok(book with { BookItems = simulationItems });
        }

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
                    endpoint = new
                    {
                        endpointId = endpoint.Id,
                        endpoint.Name,
                        x = endpoint.X,
                        y = endpoint.Y,
                        theta = Math.PI
                    }
                }
            } }
        });
    }

    /// <summary>Tìm địa điểm trong bộ dữ liệu fixture Gazebo.</summary>
    [HttpGet("simulation/facilities/search")]
    public async Task<IActionResult> SearchSimulationFacilities([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var endpoint = await dataService.GetSimulationEndpointAsync(cancellationToken);
        if (endpoint is null) return Conflict(new { message = "Simulation point of interest is not configured." });
        var mapName = (await dataService.GetMapAsync(endpoint.MapId, cancellationToken))?.Name ?? "Simulation map";
        var fixture = new { facilityId = SimulationFacilityId, name = "Information desk (simulation)", type = "SERVICE", mapId = endpoint.MapId, mapName };
        var term = query?.Trim();
        var facilities = string.IsNullOrEmpty(term) || fixture.name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || "information help desk service".Contains(term, StringComparison.OrdinalIgnoreCase)
            ? new[] { fixture } : Array.Empty<object>();
        // Keep the simulation usable when an admin enters a search term outside the fixture vocabulary.
        if (facilities.Length == 0) facilities = [fixture];
        return Ok(new { facilities });
    }

    /// <summary>Lấy vị trí chi tiết của địa điểm mô phỏng.</summary>
    [HttpGet("simulation/facilities/{facilityId:guid}")]
    public async Task<IActionResult> GetSimulationFacility(Guid facilityId, CancellationToken cancellationToken)
    {
        if (facilityId != SimulationFacilityId) return NotFound();
        var endpoint = await dataService.GetSimulationEndpointAsync(cancellationToken);
        if (endpoint is null) return Conflict(new { message = "Simulation point of interest is not configured." });
        var map = await dataService.GetMapAsync(endpoint.MapId, cancellationToken);
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
                endpoint = new { endpointId = endpoint.Id, endpoint.Name, x = endpoint.X, y = endpoint.Y, theta = endpoint.Theta }
            }
        });
    }

    /// <summary>Ghi nhận một barcode đã quét trong nhiệm vụ kiểm kê.</summary>
    [HttpPost("inventory/scan")]
    public async Task<IActionResult> RecordInventoryScan(InventoryScanRequest request, CancellationToken cancellationToken)
    {
        if (request.JobId == Guid.Empty || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { message = "jobId and qrCode are required." });
        var task = await dataService.GetJobAsync(request.JobId, cancellationToken);
        var runtimeId = task?.RuntimeId ?? request.RuntimeId;
        if (task is null && !IsActiveSimulation(request.JobId, runtimeId))
            return NotFound(new { message = "Job task not found." });
        if (task is not null && !MatchesRuntime(runtimeId, request.RuntimeId))
            return Conflict(new { message = "Runtime does not match the job." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            && !IsActiveSimulation(request.JobId, runtimeId))
            return Conflict(new { message = "No active in-memory simulation exists for this job." });

        var barcode = request.Code!.Trim();
        if (runtimeId == "real" && string.IsNullOrWhiteSpace(request.ShelfCode))
            return BadRequest(new { message = "shelfCode is required for a physical inventory scan." });
        if (request.LevelNumber is not null and not (1 or 2))
            return BadRequest(new { message = "levelNumber must be 1 or 2." });
        var shelfLevelId = runtimeId == "sim" ? null : await dataService.ResolveShelfLevelAsync(request.ShelfCode, request.LevelNumber, cancellationToken);
        if (runtimeId == "real" && shelfLevelId is null)
            return BadRequest(new { message = $"No active shelf level was found for shelf '{request.ShelfCode}'." });
        var item = runtimeId == "sim" ? null : await dataService.GetInventoryBookAsync(barcode, cancellationToken);
        string? fixtureShelf = null;
        var fixtureKnown = runtimeId == "sim" && SimulationBarcodes.TryGetValue(barcode, out fixtureShelf);
        var known = runtimeId == "sim" ? fixtureKnown : item is not null;
        var expectedShelfLevelId = item?.ExpectedShelfLevelId;
        var expectedShelfCode = runtimeId == "sim" ? (fixtureKnown ? fixtureShelf : null) : null;
        var misplaced = runtimeId == "sim"
            ? fixtureKnown && !string.Equals(fixtureShelf, request.ShelfCode, StringComparison.OrdinalIgnoreCase)
            : known && shelfLevelId is Guid scannedShelf && expectedShelfLevelId is Guid expected && scannedShelf != expected;
        if (expectedShelfLevelId is not null) expectedShelfCode = item?.ExpectedShelfCode;

        if (runtimeId == "real" && item is not null)
        {
            await dataService.RecordInventoryAsync(barcode, shelfLevelId, cancellationToken);
        }

        var scan = new
        {
            barcode,
            qrCode = barcode,
            shelfCode = request.ShelfCode,
            shelfLevelId,
            levelNumber = request.LevelNumber,
            known,
            misplaced,
            expectedShelfCode,
            runtimeId,
            scannedAt = DateTime.UtcNow
        };
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
        {
            SimulationWorkflowDataStore.AddInventoryScan(request.JobId, scan);
            return Ok(scan);
        }
        await dataService.AddJobEventAsync(request.JobId, "INVENTORY_SCAN", JsonSerializer.Serialize(scan), cancellationToken);
        return Ok(scan);
    }

    /// <summary>Lấy các barcode đã quét trong nhiệm vụ kiểm kê đang chạy hoặc đã hoàn tất.</summary>
    [HttpGet("inventory/scans/{jobId:guid}")]
    public async Task<IActionResult> GetInventoryScans(
        Guid jobId,
        [FromQuery] string? runtimeId,
        CancellationToken cancellationToken)
    {
        var task = await dataService.GetJobAsync(jobId, cancellationToken);
        runtimeId = task?.RuntimeId ?? (string.IsNullOrWhiteSpace(runtimeId) ? "sim" : runtimeId.Trim());
        if (task is null && !IsActiveSimulation(jobId, runtimeId))
            return NotFound(new { message = "Job task not found." });
        if (task is not null && !MatchesRuntime(task.RuntimeId, runtimeId))
            return Conflict(new { message = "Runtime does not match the job." });

        var isSimulation = string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase);
        var scanData = isSimulation
            ? SimulationWorkflowDataStore.GetInventoryScans(jobId)
                .Select(scan => scan.GetRawText())
                .Concat(InventoryWorkflowRuntimeStore.GetScans(jobId))
                .ToArray()
            : await dataService.GetJobEventDataAsync(jobId, "INVENTORY_SCAN", cancellationToken);
        var scans = new List<JsonElement>();
        foreach (var value in scanData)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            try
            {
                using var document = JsonDocument.Parse(value);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object) continue;

                if (TryGetProperty(root, "barcode", out _))
                {
                    scans.Add(root.Clone());
                    continue;
                }

                if (!TryGetProperty(root, "scanned_items", out var items)
                    || items.ValueKind != JsonValueKind.Array)
                    continue;

                var shelfCode = TryGetProperty(root, "shelf_code", out var shelfCodeValue)
                    && shelfCodeValue.ValueKind == JsonValueKind.String
                    ? shelfCodeValue.GetString()
                    : null;
                var scannedAt = TryGetProperty(root, "scanned_at", out var scannedAtValue)
                    && scannedAtValue.ValueKind == JsonValueKind.String
                    ? scannedAtValue.GetString()
                    : DateTime.UtcNow.ToString("O");
                var verificationByBarcode = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
                if (TryGetProperty(root, "verified_book_items", out var verifiedItems)
                    && verifiedItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var verifiedItem in verifiedItems.EnumerateArray())
                    {
                        if (TryGetProperty(verifiedItem, "qr_code", out var verifiedBarcode)
                            && verifiedBarcode.ValueKind == JsonValueKind.String
                            && !string.IsNullOrWhiteSpace(verifiedBarcode.GetString()))
                            verificationByBarcode[verifiedBarcode.GetString()!] = verifiedItem;
                    }
                }

                foreach (var item in items.EnumerateArray())
                {
                    if (!TryGetProperty(item, "qr_code", out var barcodeValue)
                        || barcodeValue.ValueKind != JsonValueKind.String
                        || string.IsNullOrWhiteSpace(barcodeValue.GetString()))
                        continue;

                    var barcode = barcodeValue.GetString()!;
                    verificationByBarcode.TryGetValue(barcode, out var verification);
                    var hasVerification = verification.ValueKind == JsonValueKind.Object;
                    var status = hasVerification && TryGetProperty(verification, "status", out var statusValue)
                        && statusValue.ValueKind == JsonValueKind.String
                        ? statusValue.GetString()
                        : "SCANNED";
                    var known = hasVerification && TryGetProperty(verification, "known", out var knownValue)
                        && knownValue.ValueKind == JsonValueKind.True;
                    var misplaced = hasVerification && TryGetProperty(verification, "misplaced", out var misplacedValue)
                        && misplacedValue.ValueKind == JsonValueKind.True;
                    var levelNumber = TryGetProperty(item, "level_number", out var levelValue)
                        && levelValue.TryGetInt32(out var level)
                        ? level
                        : (int?)null;
                    var shelfLevelId = hasVerification && TryGetProperty(verification, "actual_shelf_level_id", out var actualLevelValue)
                        && actualLevelValue.ValueKind == JsonValueKind.String
                        ? actualLevelValue.GetString()
                        : null;

                    scans.Add(JsonSerializer.SerializeToElement(new
                    {
                        barcode,
                        qrCode = barcode,
                        shelfCode,
                        shelfLevelId,
                        levelNumber,
                        known,
                        misplaced,
                        expectedShelfCode = (string?)null,
                        runtimeId,
                        scannedAt,
                        status
                    }));
                }
            }
            catch (JsonException)
            {
                // Ignore malformed historical scan events.
            }
        }

        return Ok(scans);
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    /// <summary>Creates a misplaced-book issue for a previously recorded inventory scan.</summary>
    [HttpPost("inventory/issues")]
    public async Task<IActionResult> AddInventoryIssue(InventoryIssueRequest request, CancellationToken cancellationToken)
    {
        if (request.JobId == Guid.Empty || string.IsNullOrWhiteSpace(request.Barcode)
            || string.IsNullOrWhiteSpace(request.RuntimeId) || request.ScannedAt == default)
            return BadRequest(new { message = "jobId, runtimeId, barcode and scannedAt are required." });
        if (!string.Equals(request.IssueType?.Trim(), "MISPLACED", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Only issueType MISPLACED is supported." });

        var task = await dataService.GetJobAsync(request.JobId, cancellationToken);
        var runtimeId = task?.RuntimeId ?? request.RuntimeId;
        if (task is null && !IsActiveSimulation(request.JobId, runtimeId))
            return NotFound(new { message = "Job task not found." });
        if (task is not null && !MatchesRuntime(runtimeId, request.RuntimeId))
            return Conflict(new { message = "Runtime does not match the job." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            && !IsActiveSimulation(request.JobId, runtimeId))
            return Conflict(new { message = "No active in-memory simulation exists for this job." });

        var scanEvents = string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            ? SimulationWorkflowDataStore.GetInventoryScans(request.JobId).Select(scan => scan.GetRawText()).ToArray()
            : await dataService.GetJobEventDataAsync(request.JobId, "INVENTORY_SCAN", cancellationToken);
        JsonElement matchingScan = default;
        foreach (var scanData in scanEvents)
        {
            if (string.IsNullOrWhiteSpace(scanData)) continue;
            try
            {
                using var document = JsonDocument.Parse(scanData);
                var root = document.RootElement;
                if (root.TryGetProperty("barcode", out var barcode)
                    && string.Equals(barcode.GetString(), request.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)
                    && root.TryGetProperty("scannedAt", out var scannedAt)
                    && DateTimeOffset.TryParse(scannedAt.GetString(), out var recordedAt)
                    && recordedAt.ToUniversalTime() == request.ScannedAt.ToUniversalTime())
                    matchingScan = root.Clone();
            }
            catch (JsonException)
            {
                // Ignore malformed historical scan events.
            }
        }

        if (matchingScan.ValueKind != JsonValueKind.Object
            || !matchingScan.TryGetProperty("misplaced", out var misplaced)
            || misplaced.ValueKind != JsonValueKind.True)
            return Conflict(new { message = "The referenced inventory scan is not marked as misplaced." });

        var existingIssues = string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            ? SimulationWorkflowDataStore.GetInventoryIssues(request.JobId).Select(issue => issue.GetRawText()).ToArray()
            : await dataService.GetJobEventDataAsync(request.JobId, "INVENTORY_ISSUE", cancellationToken);
        foreach (var issueData in existingIssues)
        {
            if (string.IsNullOrWhiteSpace(issueData)) continue;
            try
            {
                using var document = JsonDocument.Parse(issueData);
                var issue = document.RootElement;
                if (issue.TryGetProperty("barcode", out var barcode)
                    && string.Equals(barcode.GetString(), request.Barcode.Trim(), StringComparison.OrdinalIgnoreCase)
                    && issue.TryGetProperty("scannedAt", out var scannedAt)
                    && DateTimeOffset.TryParse(scannedAt.GetString(), out var recordedAt)
                    && recordedAt.ToUniversalTime() == request.ScannedAt.ToUniversalTime())
                    return Ok(issue.Clone());
            }
            catch (JsonException)
            {
                // Ignore malformed historical issue events.
            }
        }

        var issueRecord = new
        {
            issueId = Guid.NewGuid(),
            issueType = "MISPLACED",
            jobId = request.JobId,
            runtimeId,
            barcode = request.Barcode.Trim(),
            actualShelfCode = matchingScan.TryGetProperty("shelfCode", out var shelfCode) ? shelfCode.GetString() : null,
            expectedShelfCode = matchingScan.TryGetProperty("expectedShelfCode", out var expectedShelfCode) ? expectedShelfCode.GetString() : null,
            scannedAt = request.ScannedAt.ToUniversalTime(),
            createdAtUtc = DateTime.UtcNow
        };
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            SimulationWorkflowDataStore.AddInventoryIssue(request.JobId, issueRecord);
        else
            await dataService.AddJobEventAsync(request.JobId, "INVENTORY_ISSUE", JsonSerializer.Serialize(issueRecord), cancellationToken);

        return StatusCode(StatusCodes.Status201Created, issueRecord);
    }

    /// <summary>Tổng hợp các lượt quét và lưu/trả báo cáo kiểm kê.</summary>
    [HttpPost("inventory/reports")]
    public async Task<IActionResult> FinalizeInventory(InventoryFinalizeRequest request, CancellationToken cancellationToken)
    {
        var task = await dataService.GetJobAsync(request.JobId, cancellationToken);
        var runtimeId = task?.RuntimeId ?? request.RuntimeId;
        if (task is null && !IsActiveSimulation(request.JobId, runtimeId))
            return NotFound(new { message = "Job task not found." });
        if (task is not null && !MatchesRuntime(runtimeId, request.RuntimeId))
            return Conflict(new { message = "Runtime does not match the job." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            && !IsActiveSimulation(request.JobId, runtimeId))
            return Conflict(new { message = "No active in-memory simulation exists for this job." });

        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
        {
            var scans = SimulationWorkflowDataStore.GetInventoryScans(request.JobId);
            var barcodes = scans
                .Where(scan => scan.TryGetProperty("barcode", out var value) && value.ValueKind == JsonValueKind.String)
                .Select(scan => scan.GetProperty("barcode").GetString()!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var simulationMisplacedCount = scans.Count(scan => scan.TryGetProperty("misplaced", out var value) && value.ValueKind == JsonValueKind.True);
            var simulationReportType = string.IsNullOrWhiteSpace(request.ReportType) ? "INVENTORY" : request.ReportType.Trim().ToUpperInvariant();
            return Ok(new
            {
                reportId = Guid.NewGuid(),
                reportType = $"SIMULATION_{simulationReportType}",
                runtimeId,
                totalBooksScanned = barcodes.Count,
                totalMisplaced = simulationMisplacedCount,
                totalMissing = 0
            });
        }

        var events = await dataService.GetJobEventDataAsync(request.JobId, "INVENTORY_SCAN", cancellationToken);
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
        if (runtimeId == "real" && scannedShelfLevels.Count > 0)
        {
            var shelfLevelIds = scannedShelfLevels.ToArray();
            var barcodeArray = scannedBarcodes.ToArray();
            missingCount = await dataService.CountMissingBooksAsync(shelfLevelIds, barcodeArray, cancellationToken);
        }

        var reportType = string.IsNullOrWhiteSpace(request.ReportType) ? "INVENTORY" : request.ReportType.Trim().ToUpperInvariant();
        var storedReportType = reportType;
        var eventData = JsonSerializer.Serialize(new
        {
            runtimeId,
            totalBooksScanned = scannedBarcodes.Count,
            totalMisplaced = misplacedCount,
            totalMissing = missingCount
        });
        var reportId = await dataService.SaveInventoryReportAsync(request.JobId,
            request.ScopeType ?? "SHELF", scannedBarcodes.Count, Math.Max(0, scannedBarcodes.Count - misplacedCount),
            misplacedCount, missingCount, eventData, cancellationToken);
        return Ok(new { reportId, reportType = storedReportType, runtimeId,
            totalBooksScanned = scannedBarcodes.Count, totalMisplaced = misplacedCount, totalMissing = missingCount });
    }

    /// <summary>Ghi nhận một quan sát trong nhiệm vụ tuần tra.</summary>
    [HttpPost("patrol/observations")]
    public async Task<IActionResult> RecordPatrolObservation(PatrolObservationRequest request, CancellationToken cancellationToken)
    {
        var task = await dataService.GetJobAsync(request.JobId, cancellationToken);
        var runtimeId = task?.RuntimeId ?? request.RuntimeId;
        if (task is null && !IsActiveSimulation(request.JobId, runtimeId))
            return NotFound(new { message = "Job task not found." });
        if (task is not null && !MatchesRuntime(runtimeId, request.RuntimeId))
            return Conflict(new { message = "Runtime does not match the job." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            && !IsActiveSimulation(request.JobId, runtimeId))
            return Conflict(new { message = "No active in-memory simulation exists for this job." });
        if (request.Observation.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return BadRequest(new { message = "observation is required." });
        var category = ReadObservationCategory(request.Observation);
        var data = JsonSerializer.Serialize(new { category, observation = request.Observation, runtimeId, observedAt = DateTime.UtcNow });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
        {
            SimulationWorkflowDataStore.AddPatrolCategory(request.JobId, category);
            return Ok(new { category, runtimeId });
        }
        await dataService.AddJobEventAsync(request.JobId, "PATROL_OBSERVATION", data, cancellationToken);
        return Ok(new { category, runtimeId });
    }

    /// <summary>Tổng hợp quan sát và lưu/trả báo cáo tuần tra.</summary>
    [HttpPost("patrol/reports")]
    public async Task<IActionResult> FinalizePatrol(PatrolFinalizeRequest request, CancellationToken cancellationToken)
    {
        var task = await dataService.GetJobAsync(request.JobId, cancellationToken);
        var runtimeId = task?.RuntimeId ?? request.RuntimeId;
        if (task is null && !IsActiveSimulation(request.JobId, runtimeId))
            return NotFound(new { message = "Job task not found." });
        if (task is not null && !MatchesRuntime(runtimeId, request.RuntimeId))
            return Conflict(new { message = "Runtime does not match the job." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
            && !IsActiveSimulation(request.JobId, runtimeId))
            return Conflict(new { message = "No active in-memory simulation exists for this job." });
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
        {
            var simulationCategories = SimulationWorkflowDataStore.GetPatrolCategories(request.JobId);
            var simulationFallen = simulationCategories.Count(item => item == "FALLEN_BOOK");
            var simulationObstacles = simulationCategories.Count(item => item == "OBSTACLE");
            var simulationWarnings = simulationCategories.Count(item => item is "WARNING" or "OTHER_WARNING");
            var simulationReportId = Guid.NewGuid();
            var simulationReport = new
            {
                reportId = simulationReportId,
                reportType = "SIMULATION_PATROL",
                runtimeId,
                observationCount = simulationCategories.Count,
                fallenBookCount = simulationFallen,
                unknownObstacleCount = simulationObstacles,
                otherWarningCount = simulationWarnings
            };
            await patrolNotifications.PublishPatrolCompletedAsync(new PatrolCompletedNotification(
                request.JobId, simulationReportId, simulationReport.reportType, runtimeId,
                simulationReport.observationCount, simulationFallen, simulationObstacles, simulationWarnings,
                DateTime.UtcNow), cancellationToken);
            return Ok(simulationReport);
        }
        var observationData = await dataService.GetJobEventDataAsync(request.JobId, "PATROL_OBSERVATION", cancellationToken);
        var categories = observationData.Select(ReadStoredObservationCategory).Where(item => item is not null).ToList();
        var fallen = categories.Count(item => item == "FALLEN_BOOK");
        var obstacles = categories.Count(item => item == "OBSTACLE");
        var warnings = categories.Count(item => item is "WARNING" or "OTHER_WARNING");
        var storedType = "PATROL";
        var duration = task?.StartedAtUtc is DateTime startedAtUtc
            ? Math.Max(0, (int)(DateTime.UtcNow - startedAtUtc).TotalSeconds)
            : 0;
        var eventData = JsonSerializer.Serialize(new
        {
            runtimeId,
            observationCount = categories.Count,
            fallenBookCount = fallen,
            unknownObstacleCount = obstacles,
            otherWarningCount = warnings
        });
        var reportId = await dataService.SavePatrolReportAsync(request.JobId, duration, categories.Count,
            warnings, eventData, cancellationToken);
        await patrolNotifications.PublishPatrolCompletedAsync(new PatrolCompletedNotification(
            request.JobId, reportId, storedType, runtimeId, categories.Count, fallen, obstacles, warnings,
            DateTime.UtcNow), cancellationToken);
        return Ok(new { reportId, reportType = storedType, runtimeId,
            observationCount = categories.Count, fallenBookCount = fallen, obstacleCount = obstacles, warningCount = warnings });
    }

    private static bool MatchesRuntime(string assignedRuntime, string requestedRuntime) =>
        string.Equals(assignedRuntime, requestedRuntime, StringComparison.OrdinalIgnoreCase);

    private static bool IsActiveSimulation(Guid jobId, string runtimeId) =>
        string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase)
        && SimulationExecutionStore.TryGetExecution(jobId, out _, out var assignedRuntime)
        && string.Equals(assignedRuntime, "sim", StringComparison.OrdinalIgnoreCase);

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
