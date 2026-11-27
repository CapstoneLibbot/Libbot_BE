using LiBot.Domain.Common;

namespace LiBot.Domain.Job;

public class JobStep : Entity
{
    public Guid JobTaskId { get; private set; }
    public Guid TaskStepId { get; private set; }
    public int Sequence { get; private set; }
    public string StepType { get; private set; }
    public string Status { get; private set; }
    public string Configuration { get; private set; }

    public JobStep(Guid jobTaskId, Guid taskStepId, int sequence, string stepType, string configuration,
        string status = "Pending")
    {
        if (jobTaskId == Guid.Empty)
            throw new ArgumentException("Job task id is required.", nameof(jobTaskId));
        if (taskStepId == Guid.Empty)
            throw new ArgumentException("Task step id is required.", nameof(taskStepId));
        if (sequence < 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));
        if (string.IsNullOrWhiteSpace(stepType))
            throw new ArgumentException("Step type is required.", nameof(stepType));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Job step status is required.", nameof(status));
        if (string.IsNullOrWhiteSpace(configuration))
            throw new ArgumentException("Job step configuration is required.", nameof(configuration));

        JobTaskId = jobTaskId;
        TaskStepId = taskStepId;
        Sequence = sequence;
        StepType = stepType.Trim();
        Status = status.Trim();
        Configuration = configuration;
    }

    public void UpdateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Job step status is required.", nameof(status));
        Status = status.Trim();
    }

    public void PrepareForRetry() => Status = "Pending";
}
