using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class PatrolReport : Entity
{
    public Guid JobId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("DurationSec")]
    public int DurationSeconds { get; private set; }
    public int ObservationCount { get; private set; }
    public int WarningCount { get; private set; }
    public string? Data { get; private set; }

    public PatrolReport(Guid jobId, int durationSeconds, int observationCount, int warningCount, string? data,
        DateTime? startedAt = null, DateTime? completedAt = null)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (durationSeconds < 0 || observationCount < 0 || warningCount < 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Report values cannot be negative.");

        JobId = jobId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        DurationSeconds = durationSeconds;
        ObservationCount = observationCount;
        WarningCount = warningCount;
        Data = data?.Trim();
    }
}

