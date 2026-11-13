using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class InventoryReportDetail : Entity
{
    public Guid ReportId { get; private set; }
    public Guid BookItemId { get; private set; }
    public Guid? ExpectedShelfLevelId { get; private set; }
    public Guid DetectedShelfLevelId { get; private set; }
    public string Status { get; private set; }
    public string Result { get; private set; }

    public InventoryReportDetail(
        Guid reportId,
        Guid bookItemId,
        Guid? expectedShelfLevelId,
        Guid detectedShelfLevelId,
        string status,
        string result)
    {
        if (reportId == Guid.Empty || bookItemId == Guid.Empty || expectedShelfLevelId == Guid.Empty || detectedShelfLevelId == Guid.Empty)
            throw new ArgumentException("Identifiers are required.");
        if (string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(result))
            throw new ArgumentException("Status and result are required.");

        ReportId = reportId;
        BookItemId = bookItemId;
        ExpectedShelfLevelId = expectedShelfLevelId;
        DetectedShelfLevelId = detectedShelfLevelId;
        Status = status.Trim();
        Result = result.Trim();
    }
}
