using LiBot.Domain.Common;

namespace LiBot.Domain.Workflow;

public class ScheduledJobTask : Entity
{
    public Guid ScheduledJobId { get; private set; }
    public Guid WorkflowTaskId { get; private set; }

    public ScheduledJobTask(Guid scheduledJobId, Guid workflowTaskId)
    {
        if (scheduledJobId == Guid.Empty)
            throw new ArgumentException("Scheduled job id is required.", nameof(scheduledJobId));
        if (workflowTaskId == Guid.Empty)
            throw new ArgumentException("Workflow task id is required.", nameof(workflowTaskId));
        ScheduledJobId = scheduledJobId;
        WorkflowTaskId = workflowTaskId;
    }
}