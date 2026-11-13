using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class InventoryReport : Entity
{
    public Guid ReportId { get; private set; }
    public Guid? FloorId { get; private set; }
    public Guid? JobId { get; private set; }
    public string ScopeType { get; private set; }
    public int TotalShelves { get; private set; }
    public int TotalBooksScanned { get; private set; }
    public int TotalAvailable { get; private set; }
    public int TotalMisplaced { get; private set; }
    public int TotalMissing { get; private set; }
    public string? Data { get; private set; }

    public InventoryReport(Guid reportId, string scopeType, int totalShelves, int totalBooksScanned,
        int totalAvailable, int totalMisplaced, int totalMissing)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report id is required.", nameof(reportId));
        if (string.IsNullOrWhiteSpace(scopeType))
            throw new ArgumentException("Scope type is required.", nameof(scopeType));
        if (totalShelves < 0 || totalBooksScanned < 0 || totalAvailable < 0 || totalMisplaced < 0 || totalMissing < 0)
            throw new ArgumentOutOfRangeException(nameof(totalShelves), "Report totals cannot be negative.");

        ReportId = reportId;
        ScopeType = scopeType.Trim();
        TotalShelves = totalShelves;
        TotalBooksScanned = totalBooksScanned;
        TotalAvailable = totalAvailable;
        TotalMisplaced = totalMisplaced;
        TotalMissing = totalMissing;
    }

    public InventoryReport(Guid reportId, string scopeType, int totalShelves, int totalBooksScanned,
        int totalAvailable, int totalMisplaced, int totalMissing, string? data)
        : this(reportId, scopeType, totalShelves, totalBooksScanned, totalAvailable, totalMisplaced, totalMissing)
    {
        Data = data;
    }
}
