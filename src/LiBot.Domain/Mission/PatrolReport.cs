using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class PatrolReport : Entity
{
    public Guid? JobId { get; private set; }
    public int DurationSeconds { get; private set; }
    public int ObservationCount { get; private set; }
    public int WarningCount { get; private set; }
    public string Data { get; private set; }

    public PatrolReport(Guid? jobId, int durationSeconds, int observationCount, int warningCount, string data)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
        if (durationSeconds < 0 || observationCount < 0 || warningCount < 0)
            throw new ArgumentOutOfRangeException(nameof(durationSeconds), "Report values cannot be negative.");
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Report data is required.", nameof(data));

        JobId = jobId;
        DurationSeconds = durationSeconds;
        ObservationCount = observationCount;
        WarningCount = warningCount;
        Data = data.Trim();
    }
}
