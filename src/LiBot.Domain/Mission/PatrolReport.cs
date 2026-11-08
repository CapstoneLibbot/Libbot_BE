using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class PatrolReport : Entity
{
    public Guid ReportId { get; private set; }
    public Guid? JobId { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int DurationSeconds { get; private set; }
    public int ObservationCount { get; private set; }
    public int WarningCount { get; private set; }
    public int FallenBookCount { get; private set; }
    public int UnknownObstacleCount { get; private set; }
    public int OtherWarningCount { get; private set; }
    public string PatrolStatus { get; private set; }
    public string? Data { get; private set; }

    public PatrolReport(Guid reportId, int durationSeconds, int observationCount, int warningCount,
        int fallenBookCount, int unknownObstacleCount, int otherWarningCount, string patrolStatus)
    {
        if (reportId == Guid.Empty)
            throw new ArgumentException("Report id is required.", nameof(reportId));
        if (durationSeconds < 0 || observationCount < 0 || warningCount < 0 || fallenBookCount < 0 ||
            unknownObstacleCount < 0 || otherWarningCount < 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Report values cannot be negative.");
        if (string.IsNullOrWhiteSpace(patrolStatus))
            throw new ArgumentException("Patrol status is required.", nameof(patrolStatus));

        ReportId = reportId;
        DurationSeconds = durationSeconds;
        ObservationCount = observationCount;
        WarningCount = warningCount;
        FallenBookCount = fallenBookCount;
        UnknownObstacleCount = unknownObstacleCount;
        OtherWarningCount = otherWarningCount;
        PatrolStatus = patrolStatus.Trim();
    }
}
