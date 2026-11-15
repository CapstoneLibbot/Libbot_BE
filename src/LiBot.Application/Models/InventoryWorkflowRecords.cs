using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record InventoryShelfLevelRecord(
    [property: JsonPropertyName("shelf_level_id")] Guid Id,
    [property: JsonPropertyName("level_number")] int LevelNumber);

public sealed record InventoryNavigationTargetRecord(
    [property: JsonPropertyName("endpoint_id")] Guid EndpointId,
    string Name,
    double X,
    double Y,
    double Theta);

public sealed record InventoryShelfScopeRecord(
    [property: JsonPropertyName("shelf_id")] Guid ShelfId,
    [property: JsonPropertyName("shelf_code")] string ShelfCode,
    [property: JsonPropertyName("shelf_name")] string ShelfName,
    InventoryNavigationTargetRecord Endpoint,
    IReadOnlyList<InventoryShelfLevelRecord> Levels);

public sealed record InventoryBookItemRecord(
    Guid Id,
    string QrCode,
    Guid? ExpectedShelfLevelId,
    Guid? CurrentShelfLevelId,
    string Status);

public sealed record InventoryBookItemUpdate(Guid BookItemId, Guid? ShelfLevelId, string? Status);

public sealed record InventoryReportRecord(
    Guid ReportId,
    Guid MissionId,
    string ScopeType,
    int TotalShelves,
    int TotalBooksScanned,
    int TotalAvailable,
    int TotalMisplaced,
    int TotalMissing,
    string Data,
    DateTime CreatedAtUtc);

public sealed record InventoryWorkflowActionRequest(
    Guid MissionId,
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
    [property: JsonPropertyName("missionId")]
    Guid MissionId,
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
