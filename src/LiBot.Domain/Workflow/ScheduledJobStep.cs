using LiBot.Domain.Common;

namespace LiBot.Domain.Workflow;

public class ScheduledJobStep : Entity
{
    public Guid ScheduledJobTaskId { get; private set; }
    public string? Parameters { get; private set; }
    public string? Data { get; private set; }

    public ScheduledJobStep(Guid scheduledJobTaskId, string? parameters = null, string? data = null)
    {
        if (scheduledJobTaskId == Guid.Empty)
            throw new ArgumentException("Scheduled job task id is required.", nameof(scheduledJobTaskId));
        ScheduledJobTaskId = scheduledJobTaskId;
        Parameters = parameters;
        Data = data;
    }
}