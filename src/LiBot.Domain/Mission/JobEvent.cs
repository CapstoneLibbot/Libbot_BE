using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class JobEvent : Entity
{
    public Guid JobId { get; private set; }
    public string EventType { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("EventTime")]
    public DateTime EventTimeUtc { get; private set; }
    public string? Data { get; private set; }

    public JobEvent(Guid jobId, string eventType, string? data = null, DateTime eventTimeUtc = default)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("Event type is required.", nameof(eventType));

        JobId = jobId;
        EventType = eventType.Trim();
        Data = data;
        EventTimeUtc = eventTimeUtc == default ? DateTime.UtcNow : eventTimeUtc;
    }
}
