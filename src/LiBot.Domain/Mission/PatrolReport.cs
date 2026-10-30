using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class PatrolReport : Entity
{
    public Guid? JobId { get; private set; }
    public string ReportType { get; private set; }
    public string Summary { get; private set; }
    public int DurationSeconds { get; private set; }
    public int ObservationCount { get; private set; }
    public int WarningCount { get; private set; }
    public int FallenBookCount { get; private set; }
    public int UnknownObstacleCount { get; private set; }
    public int OtherWarningCount { get; private set; }
    public string PatrolStatus { get; private set; }

    public PatrolReport(Guid? jobId, string reportType, string summary, int durationSeconds, int observationCount, int warningCount,
        int fallenBookCount, int unknownObstacleCount, int otherWarningCount, string patrolStatus)
    {
        if (!jobId.HasValue || jobId.Value == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(reportType))
            throw new ArgumentException("Report type is required.", nameof(reportType));
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Report summary is required.", nameof(summary));
        if (durationSeconds < 0 || observationCount < 0 || warningCount < 0 || fallenBookCount < 0 ||
            unknownObstacleCount < 0 || otherWarningCount < 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Report values cannot be negative.");
        if (string.IsNullOrWhiteSpace(patrolStatus))
            throw new ArgumentException("Patrol status is required.", nameof(patrolStatus));

        JobId = jobId;
        ReportType = reportType.Trim();
        Summary = summary.Trim();
        DurationSeconds = durationSeconds;
        ObservationCount = observationCount;
        WarningCount = warningCount;
        FallenBookCount = fallenBookCount;
        UnknownObstacleCount = unknownObstacleCount;
        OtherWarningCount = otherWarningCount;
        PatrolStatus = patrolStatus.Trim();
    }
}
