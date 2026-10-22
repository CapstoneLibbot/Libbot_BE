using LiBot.Domain.Common;

namespace LiBot.Domain.Mission;

public class JobTask : Entity
{
    public Guid JobId { get; private set; }
    public Guid? RobotId { get; private set; }
    public string RuntimeId { get; private set; } = "real";
    public string? ExecutionId { get; private set; }
    public string? ExecutionState { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public JobTask(Guid jobId, Guid? robotId = null, string runtimeId = "real")
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        JobId = jobId;
        RobotId = robotId;
        RuntimeId = string.IsNullOrWhiteSpace(runtimeId) ? "real" : runtimeId.Trim().ToLowerInvariant();
    }

    public void SetExecutionId(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution id is required.", nameof(executionId));
        ExecutionId = executionId.Trim();
    }

    public void PrepareForRetry()
    {
        ExecutionId = null;
        ExecutionState = null;
        StartedAtUtc = null;
        CompletedAtUtc = null;
    }

    public void UpdateExecutionState(string executionState) => ExecutionState = executionState;

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
