using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;

namespace LiBot.Infrastructure.Workflow;

public sealed class InventoryWorkflowService(IInventoryWorkflowRepository repository) : IInventoryWorkflowService
{
    private const string ScanEventType = "INVENTORY_SCAN";
    private const string IdempotencyEventType = "INVENTORY_ACTION_IDEMPOTENCY";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ActionLocks = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<InventoryWorkflowActionResponse> ExecuteAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        CancellationToken cancellationToken)
    {
        var action = request.Action.Trim().ToUpperInvariant();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            action + ":" + request.Payload.GetRawText())));
        var cacheKey = $"{request.JobId:N}:{request.IdempotencyKey.Trim()}";
        var gate = ActionLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var cached = await FindCachedActionAsync(request, simulation, cancellationToken);
            if (cached is not null)
            {
                if (!string.Equals(cached.Fingerprint, fingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException("idempotencyKey was already used with a different action or payload.");
                return cached.Response;
            }

            var (data, affectedRecords) = action switch
            {
                "RESOLVE_INVENTORY_SCOPE" => await ResolveScopeAsync(request.Payload, cancellationToken),
                "ADVANCE_INVENTORY_SCOPE" => AdvanceScope(request.Payload),
                "VERIFY_BOOK" => await VerifyBooksAsync(request, simulation, cancellationToken),
                "UPDATE_BOOK_ITEM_INVENTORY_STATUS" => await UpdateStatusesAsync(request.Payload, simulation, cancellationToken),
                "GENERATE_REPORT" => await GenerateReportAsync(request, simulation, cancellationToken),
                _ => throw new ArgumentException($"Unsupported inventory action '{action}'.")
            };

            var response = new InventoryWorkflowActionResponse("SUCCEEDED", data, Guid.NewGuid(), affectedRecords);
            await SaveCachedActionAsync(request, simulation, fingerprint, response, cancellationToken);
            return response;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<InventoryWorkflowReportResponse?> GetReportAsync(
        Guid jobId,
        string runtimeId,
        CancellationToken cancellationToken)
    {
        if (string.Equals(runtimeId, "sim", StringComparison.OrdinalIgnoreCase))
            return InventoryWorkflowRuntimeStore.GetReport<InventoryWorkflowReportResponse>(jobId);

        var record = await repository.GetInventoryReportAsync(jobId, cancellationToken);
        if (record is null) return null;
        using var document = JsonDocument.Parse(record.Data);
        var root = document.RootElement;
        var details = TryGet(root, "details", out var value) ? value.Clone() : JsonDocument.Parse("{}").RootElement.Clone();
        var unknown = TryGet(root, "totalUnknown", out value) && value.TryGetInt32(out var count) ? count : 0;
        return new InventoryWorkflowReportResponse(record.ReportId, jobId, "INVENTORY", "real",
            record.TotalShelves, record.TotalBooksScanned, record.TotalAvailable, record.TotalMisplaced,
            record.TotalMissing, unknown, details, record.CreatedAtUtc);
    }

    private async Task<(object Data, int AffectedRecords)> ResolveScopeAsync(
        JsonElement payload,
        CancellationToken cancellationToken)
    {
        var floorId = ReadGuid(payload, "floor_id", "floorId");
        var mapId = ReadOptionalGuid(payload, "map_id", "mapId");
        var shelfIds = ReadGuidArray(payload, "target_shelves", "targetShelves");
        var shelves = await repository.ResolveScopeAsync(floorId, mapId, shelfIds, cancellationToken);
        return (new { shelves }, shelves.Count);
    }

    private static (object Data, int AffectedRecords) AdvanceScope(JsonElement payload)
    {
        var shelvesElement = ReadRequired(payload, "shelves", "target_shelves", "targetShelves");
        var shelves = JsonSerializer.Deserialize<InventoryShelfScopeRecord[]>(shelvesElement.GetRawText(), JsonOptions)
            ?? Array.Empty<InventoryShelfScopeRecord>();
        var currentIndex = ReadOptionalInt(payload, -1, "current_shelf_index", "currentShelfIndex");
        var nextIndex = currentIndex + 1;
        var hasNext = nextIndex >= 0 && nextIndex < shelves.Length;
        return (new
        {
            current_shelf_index = hasNext ? nextIndex : currentIndex,
            current_shelf = hasNext ? shelves[nextIndex] : null,
            has_next = hasNext
        }, hasNext ? 1 : 0);
    }

    private async Task<(object Data, int AffectedRecords)> VerifyBooksAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        var shelfId = ReadGuid(payload, "shelf_id", "shelfId");
        var shelf = await repository.GetShelfScopeAsync(shelfId, cancellationToken)
            ?? throw new KeyNotFoundException("The inventory shelf or its navigation endpoint was not found.");
        var scans = ReadScanItems(payload);
        var levelByNumber = shelf.Levels.ToDictionary(item => item.LevelNumber, item => item.Id);
        if (levelByNumber.Count != 2 || !levelByNumber.ContainsKey(1) || !levelByNumber.ContainsKey(2))
            throw new InvalidOperationException($"Shelf '{shelf.ShelfCode}' must have exactly levels 1 and 2 configured.");

        var codes = scans.Select(item => item.QrCode.Trim()).Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var books = await repository.GetBookItemsAsync(shelf.Levels.Select(item => item.Id).ToArray(), codes, cancellationToken);
        var booksByCode = books.GroupBy(item => item.QrCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var verified = new List<VerifiedInventoryBookItem>();
        var updates = new List<InventoryBookItemUpdate>();
        var issueDetected = false;

        foreach (var group in scans.Where(item => !string.IsNullOrWhiteSpace(item.QrCode))
                     .GroupBy(item => item.QrCode.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var locations = group.Select(item => item.LevelNumber).Distinct().ToArray();
            if (locations.Length != 1 || !levelByNumber.TryGetValue(locations[0], out var actualLevelId))
            {
                issueDetected = true;
                booksByCode.TryGetValue(group.Key, out var ambiguousBook);
                verified.Add(new VerifiedInventoryBookItem(ambiguousBook?.Id, group.Key,
                    ambiguousBook?.ExpectedShelfLevelId, null, "AMBIGUOUS", ambiguousBook is not null, true, false));
                continue;
            }

            if (!booksByCode.TryGetValue(group.Key, out var book))
            {
                issueDetected = true;
                verified.Add(new VerifiedInventoryBookItem(null, group.Key, null, actualLevelId, "UNKNOWN", false, false, false));
                continue;
            }

            var misplaced = book.ExpectedShelfLevelId != actualLevelId;
            var status = misplaced ? "MISPLACED" : "AVAILABLE";
            issueDetected |= misplaced;
            verified.Add(new VerifiedInventoryBookItem(book.Id, group.Key, book.ExpectedShelfLevelId,
                actualLevelId, status, true, misplaced, false));
            if (!simulation)
                updates.Add(new InventoryBookItemUpdate(book.Id, actualLevelId, misplaced ? null : "Available"));
        }

        var scannedCodes = scans.Select(item => item.QrCode.Trim()).Where(item => item.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var expected in books.Where(item => item.ExpectedShelfLevelId.HasValue
                     && shelf.Levels.Any(level => level.Id == item.ExpectedShelfLevelId.Value)
                     && !scannedCodes.Contains(item.QrCode)))
        {
            issueDetected = true;
            verified.Add(new VerifiedInventoryBookItem(expected.Id, expected.QrCode,
                expected.ExpectedShelfLevelId, null, "MISSING", true, false, true));
        }

        var affected = updates.Count == 0 ? 0 : await repository.UpdateBookItemsAsync(updates, cancellationToken);
        var scanEvent = new
        {
            shelf_id = shelf.ShelfId,
            shelf_code = shelf.ShelfCode,
            scanned_items = scans,
            verified_book_items = verified,
            scanned_at = DateTime.UtcNow
        };
        if (simulation)
            InventoryWorkflowRuntimeStore.AddScan(request.JobId, scanEvent);
        else
            await repository.AddJobEventAsync(request.JobId, ScanEventType,
                JsonSerializer.Serialize(scanEvent), cancellationToken);

        return (new
        {
            verified_book_items = verified,
            verification_status = issueDetected ? "ISSUES_DETECTED" : "VERIFIED",
            issue_detected = issueDetected,
            scanned_count = codes.Length
        }, affected);
    }

    private async Task<(object Data, int AffectedRecords)> UpdateStatusesAsync(
        JsonElement payload,
        bool simulation,
        CancellationToken cancellationToken)
    {
        var items = ReadVerifiedItems(payload);
        var updates = items.Where(item => item.BookItemId.HasValue && item.Known
                && item.Status is "MISPLACED" or "MISSING")
            .Select(item => new InventoryBookItemUpdate(
                item.BookItemId!.Value,
                item.ActualShelfLevelId,
                item.Missing ? "Missing" : "Misplaced"))
            .ToArray();
        var affected = simulation || updates.Length == 0
            ? 0
            : await repository.UpdateBookItemsAsync(updates, cancellationToken);
        return (new { updated_book_items = updates.Length, update_status = true, update_location = true }, affected);
    }

    private async Task<(object Data, int AffectedRecords)> GenerateReportAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        var floorId = ReadGuid(payload, "floor_id", "floorId");
        var mapId = ReadOptionalGuid(payload, "map_id", "mapId");
        var shelfIds = ReadGuidArray(payload, "target_shelves", "targetShelves");
        var shelves = await repository.ResolveScopeAsync(floorId, mapId, shelfIds, cancellationToken);
        var scans = await GetScansAsync(request.JobId, simulation, cancellationToken);
        var flattenedScans = FlattenScans(scans, shelves);
        var shelfLevelIds = shelves.SelectMany(item => item.Levels).Select(item => item.Id).Distinct().ToArray();
        var codes = flattenedScans.Select(item => item.QrCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var books = await repository.GetBookItemsAsync(shelfLevelIds, codes, cancellationToken);
        var booksByCode = books.GroupBy(item => item.QrCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var expectedInScope = books.Where(item => item.ExpectedShelfLevelId.HasValue
                && shelfLevelIds.Contains(item.ExpectedShelfLevelId.Value))
            .GroupBy(item => item.QrCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var scanByCode = flattenedScans.GroupBy(item => item.QrCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        var ambiguous = scanByCode.Where(item => item.Value.Select(scan => scan.ActualShelfLevelId).Distinct().Count() > 1)
            .Select(item => item.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var misplaced = scanByCode.Count(item => !ambiguous.Contains(item.Key)
            && booksByCode.TryGetValue(item.Key, out var book)
            && book.ExpectedShelfLevelId.HasValue
            && item.Value[0].ActualShelfLevelId != book.ExpectedShelfLevelId.Value);
        var available = scanByCode.Count(item => !ambiguous.Contains(item.Key)
            && booksByCode.TryGetValue(item.Key, out var book)
            && book.ExpectedShelfLevelId.HasValue
            && item.Value[0].ActualShelfLevelId == book.ExpectedShelfLevelId.Value);
        var missingCodes = expectedInScope.Keys.Where(code => !scanByCode.ContainsKey(code)).ToArray();
        var unknownCodes = scanByCode.Keys.Where(code => !booksByCode.TryGetValue(code, out var book)
            || !book.ExpectedShelfLevelId.HasValue).ToArray();
        var totalScanned = scanByCode.Count;

        var updates = booksByCode.Values.Where(item => item.ExpectedShelfLevelId.HasValue)
            .Select(book =>
        {
            if (!scanByCode.TryGetValue(book.QrCode, out var locations))
                return expectedInScope.ContainsKey(book.QrCode)
                    ? new InventoryBookItemUpdate(book.Id, null, "Missing")
                    : null;
            var actual = locations[0].ActualShelfLevelId;
            if (ambiguous.Contains(book.QrCode)) return new InventoryBookItemUpdate(book.Id, actual, "Misplaced");
            return new InventoryBookItemUpdate(book.Id, actual,
                actual == book.ExpectedShelfLevelId ? "Available" : "Misplaced");
        })
        .Where(item => item is not null)
        .Select(item => item!)
        .ToArray();

        if (!simulation && updates.Length > 0)
            await repository.UpdateBookItemsAsync(updates, cancellationToken);

        var details = new
        {
            shelves = shelves.Select(shelf => new
            {
                shelf_id = shelf.ShelfId,
                shelf_code = shelf.ShelfCode,
                levels = shelf.Levels.Select(level => new
                {
                    shelf_level_id = level.Id,
                    level_number = level.LevelNumber,
                    scanned_qr_codes = flattenedScans.Where(scan => scan.ShelfId == shelf.ShelfId
                            && scan.ActualShelfLevelId == level.Id)
                        .Select(scan => scan.QrCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                }).ToArray()
            }).ToArray(),
            misplaced_qr_codes = scanByCode.Where(item => booksByCode.TryGetValue(item.Key, out var book)
                    && book.ExpectedShelfLevelId.HasValue
                    && (ambiguous.Contains(item.Key) || item.Value[0].ActualShelfLevelId != book.ExpectedShelfLevelId.Value))
                .Select(item => item.Key).ToArray(),
            missing_qr_codes = missingCodes,
            unknown_qr_codes = unknownCodes,
            ambiguous_qr_codes = ambiguous.ToArray()
        };
        var createdAt = DateTime.UtcNow;
        var reportData = JsonSerializer.Serialize(new
        {
            totalUnknown = unknownCodes.Length,
            details
        });
        var reportId = simulation
            ? Guid.NewGuid()
            : await repository.SaveInventoryReportAsync(request.JobId,
                ReadOptionalString(payload, "scope_type", "scopeType") ?? "SHELVES",
                shelves.Count, totalScanned, available, misplaced, missingCodes.Length,
                reportData, cancellationToken);

        var report = new InventoryWorkflowReportResponse(reportId, request.JobId,
            simulation ? "SIMULATION_INVENTORY" : "INVENTORY", request.RuntimeId,
            shelves.Count, totalScanned, available, misplaced, missingCodes.Length, unknownCodes.Length,
            details, createdAt);
        if (simulation) InventoryWorkflowRuntimeStore.SetReport(request.JobId, report);
        return (report, shelves.Count);
    }

    private async Task<IReadOnlyList<string>> GetScansAsync(
        Guid jobId,
        bool simulation,
        CancellationToken cancellationToken)
    {
        if (simulation) return InventoryWorkflowRuntimeStore.GetScans(jobId);
        var events = await repository.GetJobEventDataAsync(jobId, ScanEventType, cancellationToken);
        return events.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!).ToArray();
    }

    private static IReadOnlyList<FlatScan> FlattenScans(
        IReadOnlyList<string> eventData,
        IReadOnlyList<InventoryShelfScopeRecord> shelves)
    {
        var shelfById = shelves.ToDictionary(item => item.ShelfId);
        var output = new List<FlatScan>();
        foreach (var json in eventData)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var shelfId = ReadGuid(root, "shelf_id", "shelfId");
            if (!shelfById.TryGetValue(shelfId, out var shelf)
                || !TryGet(root, "scanned_items", out var scanned)
                || scanned.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var scan in scanned.EnumerateArray())
            {
                var code = ReadOptionalString(scan, "qr_code", "qrCode")?.Trim();
                var levelNumber = ReadOptionalInt(scan, -1, "level_number", "levelNumber");
                var level = shelf.Levels.FirstOrDefault(item => item.LevelNumber == levelNumber);
                if (!string.IsNullOrWhiteSpace(code) && level is not null)
                    output.Add(new FlatScan(shelfId, code, level.Id));
            }
        }

        return output;
    }

    private async Task<CachedResponse?> FindCachedActionAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        CancellationToken cancellationToken)
    {
        if (simulation)
        {
            return InventoryWorkflowRuntimeStore.TryGetAction(request.JobId, request.IdempotencyKey.Trim(),
                    out var cachedFingerprint, out var json)
                ? new CachedResponse(cachedFingerprint,
                    JsonSerializer.Deserialize<InventoryWorkflowActionResponse>(json, JsonOptions)!)
                : null;
        }

        var records = await repository.GetJobEventDataAsync(request.JobId, IdempotencyEventType, cancellationToken);
        foreach (var record in records.Where(item => !string.IsNullOrWhiteSpace(item)))
        {
            using var document = JsonDocument.Parse(record!);
            var root = document.RootElement;
            if (!string.Equals(ReadOptionalString(root, "key"), request.IdempotencyKey.Trim(), StringComparison.Ordinal))
                continue;
            var response = ReadRequired(root, "response");
            return new CachedResponse(ReadOptionalString(root, "fingerprint") ?? string.Empty,
                JsonSerializer.Deserialize<InventoryWorkflowActionResponse>(response.GetRawText(), JsonOptions)!);
        }

        return null;
    }

    private async Task SaveCachedActionAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        string fingerprint,
        InventoryWorkflowActionResponse response,
        CancellationToken cancellationToken)
    {
        if (simulation)
        {
            InventoryWorkflowRuntimeStore.AddAction(request.JobId, request.IdempotencyKey.Trim(), fingerprint, response);
            return;
        }

        await repository.AddJobEventAsync(request.JobId, IdempotencyEventType,
            JsonSerializer.Serialize(new
            {
                key = request.IdempotencyKey.Trim(),
                fingerprint,
                response
            }), cancellationToken);
    }

    private static IReadOnlyList<InventoryQrScanItem> ReadScanItems(JsonElement payload)
    {
        if (!TryGet(payload, "scanned_items", out var items) || items.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("scanned_items must be an array.");
        return JsonSerializer.Deserialize<InventoryQrScanItem[]>(items.GetRawText(), JsonOptions)
            ?? Array.Empty<InventoryQrScanItem>();
    }

    private static IReadOnlyList<VerifiedInventoryBookItem> ReadVerifiedItems(JsonElement payload)
    {
        if (!TryGet(payload, "book_items", "verified_book_items", "bookItems", out var items)
            || items.ValueKind != JsonValueKind.Array)
            throw new ArgumentException("book_items must be an array.");
        return JsonSerializer.Deserialize<VerifiedInventoryBookItem[]>(items.GetRawText(), JsonOptions)
            ?? Array.Empty<VerifiedInventoryBookItem>();
    }

    private static Guid ReadGuid(JsonElement element, params string[] names)
    {
        var value = ReadOptionalString(element, names);
        return Guid.TryParse(value, out var result) && result != Guid.Empty
            ? result
            : throw new ArgumentException($"{names[0]} must be a valid identifier.");
    }

    private static Guid? ReadOptionalGuid(JsonElement element, params string[] names)
    {
        var value = ReadOptionalString(element, names);
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Guid.TryParse(value, out var result) && result != Guid.Empty
            ? result
            : throw new ArgumentException($"{names[0]} must be a valid identifier.");
    }

    private static Guid[] ReadGuidArray(JsonElement element, params string[] names)
    {
        var value = ReadRequired(element, names);
        if (value.ValueKind != JsonValueKind.Array) throw new ArgumentException($"{names[0]} must be an array.");
        var ids = new List<Guid>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var id) || id == Guid.Empty)
                throw new ArgumentException($"{names[0]} must contain valid identifiers.");
            ids.Add(id);
        }
        return ids.ToArray();
    }

    private static int ReadOptionalInt(JsonElement element, int fallback, params string[] names)
    {
        if (!TryGet(element, names, out var value) || value.ValueKind == JsonValueKind.Null) return fallback;
        return value.TryGetInt32(out var result) ? result : throw new ArgumentException($"{names[0]} must be an integer.");
    }

    private static string? ReadOptionalString(JsonElement element, params string[] names) =>
        TryGet(element, names, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static JsonElement ReadRequired(JsonElement element, params string[] names) =>
        TryGet(element, names, out var value)
            ? value
            : throw new ArgumentException($"{names[0]} is required.");

    private static bool TryGet(JsonElement element, string name, out JsonElement value) =>
        TryGet(element, new[] { name }, out value);

    private static bool TryGet(JsonElement element, string first, string second, string third, out JsonElement value) =>
        TryGet(element, new[] { first, second, third }, out value);

    private static bool TryGet(JsonElement element, string[] names, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private sealed record CachedResponse(string Fingerprint, InventoryWorkflowActionResponse Response);
    private sealed record FlatScan(Guid ShelfId, string QrCode, Guid ActualShelfLevelId);
}
