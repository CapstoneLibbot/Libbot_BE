using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class InventoryReportDetail : Entity
{
    public Guid ReportId { get; private set; }
    public Guid BookItemId { get; private set; }
    public Guid? ExpectedShelfLevelId { get; private set; }
    public Guid DetectedShelfLevelId { get; private set; }
    public InventoryReportDetailStatus Status { get; private set; }
    public InventoryReportDetailResult Result { get; private set; }

    public InventoryReportDetail(
        Guid reportId,
        Guid bookItemId,
        Guid? expectedShelfLevelId,
        Guid detectedShelfLevelId,
        InventoryReportDetailStatus status,
        InventoryReportDetailResult result)
    {
        if (reportId == Guid.Empty || bookItemId == Guid.Empty || expectedShelfLevelId == Guid.Empty || detectedShelfLevelId == Guid.Empty)
            throw new ArgumentException("Identifiers are required.");

        ReportId = reportId;
        BookItemId = bookItemId;
        ExpectedShelfLevelId = expectedShelfLevelId;
        DetectedShelfLevelId = detectedShelfLevelId;
        Status = status;
        Result = result;
    }

    public InventoryReportDetail(
        Guid reportId,
        Guid bookItemId,
        Guid? expectedShelfLevelId,
        Guid detectedShelfLevelId,
        string status,
        string result)
        : this(reportId, bookItemId, expectedShelfLevelId, detectedShelfLevelId, ParseStatus(status), ParseResult(result))
    {
    }

    private static InventoryReportDetailStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required.", nameof(status));

        if (Enum.TryParse<InventoryReportDetailStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException($"Invalid inventory report detail status '{status}'.", nameof(status));
    }

    private static InventoryReportDetailResult ParseResult(string result)
    {
        if (string.IsNullOrWhiteSpace(result))
            throw new ArgumentException("Result is required.", nameof(result));

        if (Enum.TryParse<InventoryReportDetailResult>(result.Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException($"Invalid inventory report detail result '{result}'.", nameof(result));
    }
}
