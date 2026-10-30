using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class InventoryReport : Entity
{
    public Guid? JobId { get; private set; }
    public string ReportType { get; private set; }
    public string Summary { get; private set; }
    public string ScopeType { get; private set; }
    public int TotalShelves { get; private set; }
    public int TotalBooksScanned { get; private set; }
    public int TotalAvailable { get; private set; }
    public int TotalMisplaced { get; private set; }
    public int TotalMissing { get; private set; }

    public InventoryReport(Guid? jobId, string reportType, string summary, string scopeType, int totalShelves, int totalBooksScanned,
        int totalAvailable, int totalMisplaced, int totalMissing)
    {
        if (!jobId.HasValue || jobId.Value == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Report type is required.", nameof(reportType));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Report summary is required.", nameof(summary));
        if (string.IsNullOrWhiteSpace(scopeType))
            throw new ArgumentException("Scope type is required.", nameof(scopeType));
        if (totalShelves < 0 || totalBooksScanned < 0 || totalAvailable < 0 || totalMisplaced < 0 || totalMissing < 0)
            throw new ArgumentOutOfRangeException(nameof(totalShelves), "Report totals cannot be negative.");

        JobId = jobId;
        ReportType = reportType.Trim();
        Summary = summary.Trim();
        ScopeType = scopeType.Trim();
        TotalShelves = totalShelves;
        TotalBooksScanned = totalBooksScanned;
        TotalAvailable = totalAvailable;
        TotalMisplaced = totalMisplaced;
        TotalMissing = totalMissing;
    }
}
