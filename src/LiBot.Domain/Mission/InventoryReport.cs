using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class InventoryReport : Entity
{
    public Guid? JobId { get; private set; }
    public string ScopeType { get; private set; }
    public int TotalShelves { get; private set; }
    public int TotalBooksScanned { get; private set; }
    public int TotalAvailable { get; private set; }
    public int TotalMisplaced { get; private set; }
    public int TotalMissing { get; private set; }
    public string Data { get; private set; }

    public InventoryReport(Guid? jobId, string scopeType, int totalShelves, int totalBooksScanned,
        int totalAvailable, int totalMisplaced, int totalMissing, string data)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(scopeType))
            throw new ArgumentException("Scope type is required.", nameof(scopeType));
        if (totalShelves < 0 || totalBooksScanned < 0 || totalAvailable < 0 || totalMisplaced < 0 || totalMissing < 0)
            throw new ArgumentOutOfRangeException(nameof(totalShelves), "Report totals cannot be negative.");
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Report data is required.", nameof(data));

        JobId = jobId;
        ScopeType = scopeType.Trim();
        TotalShelves = totalShelves;
        TotalBooksScanned = totalBooksScanned;
        TotalAvailable = totalAvailable;
        TotalMisplaced = totalMisplaced;
        TotalMissing = totalMissing;
        Data = data.Trim();
    }
}

