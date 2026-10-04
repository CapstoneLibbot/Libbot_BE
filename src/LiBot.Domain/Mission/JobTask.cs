using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class JobTask : Entity
{
    public Guid JobId { get; private set; }
    public Guid? RobotId { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public JobTask(Guid jobId, Guid? robotId = null)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        JobId = jobId;
        RobotId = robotId;
    }

    public void Start(DateTime? startedAtUtc = null)
    {
        if (StartedAtUtc.HasValue)
            throw new InvalidOperationException("Task has already started.");
        StartedAtUtc = startedAtUtc ?? DateTime.UtcNow;
    }

    public void Complete(DateTime? completedAtUtc = null)
    {
        if (!StartedAtUtc.HasValue)
            throw new InvalidOperationException("Task must start before it can complete.");
        if (CompletedAtUtc.HasValue)
            throw new InvalidOperationException("Task has already completed.");
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
    }
}