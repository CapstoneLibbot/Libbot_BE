using LiBot.Domain.Common;

namespace LiBot.Domain.Workflow;

public class ScheduledJob : Entity
{
    public Guid ScheduleId { get; private set; }
    public Guid JobId { get; private set; }

    public ScheduledJob(Guid scheduleId, Guid jobId)
    {
        if (scheduleId == Guid.Empty)
            throw new ArgumentException("Schedule id is required.", nameof(scheduleId));
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        ScheduleId = scheduleId;
        JobId = jobId;
    }
}