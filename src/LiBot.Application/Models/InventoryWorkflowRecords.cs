using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record InventoryShelfLevelRecord(
    [property: JsonPropertyName("shelf_level_id")] Guid Id,
    [property: JsonPropertyName("level_number")] int LevelNumber);

public sealed record InventoryNavigationTargetRecord(
    [property: JsonPropertyName("endpoint_id")] Guid EndpointId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("theta")] double Theta,
    [property: JsonPropertyName("map_id")] Guid MapId);

public sealed record InventoryShelfScopeRecord(
    [property: JsonPropertyName("shelf_id")] Guid ShelfId,
    [property: JsonPropertyName("shelf_code")] string ShelfCode,
    [property: JsonPropertyName("shelf_name")] string ShelfName,
    [property: JsonPropertyName("endpoint")] InventoryNavigationTargetRecord Endpoint,
    [property: JsonPropertyName("levels")] IReadOnlyList<InventoryShelfLevelRecord> Levels,
    [property: JsonPropertyName("start_endpoint")] InventoryNavigationTargetRecord? StartEndpoint,
    [property: JsonPropertyName("end_endpoint")] InventoryNavigationTargetRecord? EndEndpoint);

public static class InventoryEndpointValidator
{
    public static void Validate(InventoryNavigationTargetRecord start, InventoryNavigationTargetRecord end)
    {
        if (start.EndpointId == Guid.Empty || end.EndpointId == Guid.Empty)
            throw new InvalidOperationException("Shelf endpoint ID is missing.");
        if (start.MapId == Guid.Empty || start.MapId != end.MapId)
            throw new InvalidOperationException("Shelf endpoints must belong to the same map.");
        if (!double.IsFinite(start.X) || !double.IsFinite(start.Y) || !double.IsFinite(start.Theta)
            || !double.IsFinite(end.X) || !double.IsFinite(end.Y) || !double.IsFinite(end.Theta))
            throw new InvalidOperationException("Shelf endpoint pose must be finite.");
        // A shelf may be configured as a point destination by assigning the
        // same endpoint to both ends. Inventory navigation handles this as a
        // single goal and then advances to the next selected shelf.
        if (start.EndpointId == end.EndpointId)
            return;
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        var yawDifference = Math.Atan2(Math.Sin(start.Theta - end.Theta), Math.Cos(start.Theta - end.Theta));
        var forward = dx * Math.Cos(start.Theta) + dy * Math.Sin(start.Theta);
        if (!double.IsFinite(length) || !double.IsFinite(yawDifference) || !double.IsFinite(forward)
            || length < 0.02 || Math.Abs(yawDifference) > 0.1 || Math.Abs(forward) > 0.05)
            throw new InvalidOperationException("Shelf endpoints must be distinct, lateral and have matching orientations.");
    }
}

public sealed record InventoryBookItemRecord(
    Guid Id,
    string QrCode,
    Guid? ExpectedShelfLevelId,
    Guid? CurrentShelfLevelId,
    string Status);

public sealed record InventoryBookItemUpdate(Guid BookItemId, Guid? ShelfLevelId, string? Status);

public sealed record InventoryReportRecord(
    Guid ReportId,
    Guid JobId,
    string ScopeType,
    int TotalShelves,
    int TotalBooksScanned,
    int TotalAvailable,
    int TotalMisplaced,
    int TotalMissing,
    string Data,
    DateTime CreatedAtUtc);

public sealed record InventoryWorkflowActionRequest(
    Guid JobId,
    string RuntimeId,
    string ExecutionId,
    string Action,
    System.Text.Json.JsonElement Payload,
    string IdempotencyKey);

public sealed record InventoryWorkflowActionResponse(
    [property: JsonPropertyName("action_status")]
    string ActionStatus,
    [property: JsonPropertyName("data")]
    object Data,
    [property: JsonPropertyName("request_id")]
    Guid RequestId,
    [property: JsonPropertyName("affected_records")]
    int AffectedRecords);

public sealed record InventoryQrScanItem(
    [property: JsonPropertyName("qr_code")] string QrCode,
    [property: JsonPropertyName("level_number")] int LevelNumber,
    [property: JsonPropertyName("center_x")] int? CenterX = null,
    [property: JsonPropertyName("center_y")] int? CenterY = null);

public sealed record VerifiedInventoryBookItem(
    [property: JsonPropertyName("book_item_id")] Guid? BookItemId,
    [property: JsonPropertyName("qr_code")] string QrCode,
    [property: JsonPropertyName("expected_shelf_level_id")] Guid? ExpectedShelfLevelId,
    [property: JsonPropertyName("actual_shelf_level_id")] Guid? ActualShelfLevelId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("known")] bool Known,
    [property: JsonPropertyName("misplaced")] bool Misplaced,
    [property: JsonPropertyName("missing")] bool Missing);

public sealed record InventoryWorkflowReportResponse(
    [property: JsonPropertyName("reportId")]
    Guid ReportId,
    [property: JsonPropertyName("jobId")]
    Guid JobId,
    [property: JsonPropertyName("reportType")]
    string ReportType,
    [property: JsonPropertyName("runtimeId")]
    string RuntimeId,
    [property: JsonPropertyName("totalShelves")]
    int TotalShelves,
    [property: JsonPropertyName("totalBooksScanned")]
    int TotalBooksScanned,
    [property: JsonPropertyName("totalAvailable")]
    int TotalAvailable,
    [property: JsonPropertyName("totalMisplaced")]
    int TotalMisplaced,
    [property: JsonPropertyName("totalMissing")]
    int TotalMissing,
    [property: JsonPropertyName("totalUnknown")]
    int TotalUnknown,
    [property: JsonPropertyName("details")]
    object Details,
    [property: JsonPropertyName("createdAtUtc")]
    DateTime CreatedAtUtc);
