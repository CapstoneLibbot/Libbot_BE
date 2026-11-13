using LiBot.Domain.Common;

namespace LiBot.Domain.Workflow;

public class Schedule : Entity
{
    public Guid? CreatedBy { get; private set; }
    public Guid? WorkflowId { get; private set; }
    public string? Name { get; private set; }
    public string? Data { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string ScheduleType { get; private set; }
    public TimeOnly ScheduleTime { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateTime? LastRunAtUtc { get; private set; }
    public DateTime? NextRunAtUtc { get; private set; }

    public Schedule(string scheduleType, TimeOnly scheduleTime, DateOnly startDate,
        DateOnly? endDate = null, Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(scheduleType))
            throw new ArgumentException("Schedule type is required.", nameof(scheduleType));
        if (endDate < startDate)
            throw new ArgumentException("End date cannot precede start date.", nameof(endDate));

        ScheduleType = scheduleType.Trim();
        ScheduleTime = scheduleTime;
        StartDate = startDate;
        EndDate = endDate;
        CreatedBy = createdBy;
    }

    public Schedule(string name, string? data, string scheduleType, TimeOnly scheduleTime, DateOnly startDate,
        DateOnly? endDate = null, Guid? createdBy = null)
        : this(scheduleType, scheduleTime, startDate, endDate, createdBy)
    {
        Name = string.IsNullOrWhiteSpace(name) ? scheduleType.Trim() : name.Trim();
        Data = data;
    }

    public void RecordRun(DateTime runAtUtc, DateTime? nextRunAtUtc)
    {
        LastRunAtUtc = runAtUtc;
        NextRunAtUtc = nextRunAtUtc;
    }
}
