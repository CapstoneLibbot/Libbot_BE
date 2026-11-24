using LiBot.Domain.Mission;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Persistence;

internal static class JobEventPersistence
{
    public const string JobTaskStateEventType = "__JOB_TASK_STATE";

    public static async Task<JobEvent> CreateForMissionAsync(
        LiBotDbContext db,
        Guid jobId,
        string eventType,
        string? data,
        CancellationToken cancellationToken)
    {
        var jobStepId = await (from step in db.JobSteps.AsNoTracking()
                               join task in db.JobTasks.AsNoTracking() on step.JobTaskId equals task.Id
                               where task.JobId == jobId
                               orderby step.Sequence, step.Id
                               select step.Id).FirstOrDefaultAsync(cancellationToken);
        if (jobStepId == Guid.Empty)
            throw new InvalidOperationException($"Job '{jobId}' has no job step for its event.");

        return new JobEvent(jobStepId, eventType, data);
    }

    public static async Task<IReadOnlyList<string?>> GetEventDataAsync(
        LiBotDbContext db,
        Guid jobId,
        string eventType,
        CancellationToken cancellationToken)
    {
        var stored = await (from item in db.JobEvents.AsNoTracking()
                            join step in db.JobSteps.AsNoTracking() on item.JobStepId equals step.Id
                            join task in db.JobTasks.AsNoTracking() on step.JobTaskId equals task.Id
                            where task.JobId == jobId
                            orderby item.EventTimeUtc, item.Id
                            select item.Data).ToListAsync(cancellationToken);

        return stored.Select(JobEventDataEnvelope.Read)
            .Where(item => string.Equals(item.EventType, eventType, StringComparison.Ordinal))
            .Select(item => item.Data)
            .ToArray();
    }

    public static async Task HydrateJobTaskAsync(
        LiBotDbContext db,
        JobTask task,
        CancellationToken cancellationToken)
    {
        var stored = await (from item in db.JobEvents.AsNoTracking()
                            join step in db.JobSteps.AsNoTracking() on item.JobStepId equals step.Id
                            where step.JobTaskId == task.Id
                            orderby item.EventTimeUtc descending, item.Id descending
                            select item.Data).ToListAsync(cancellationToken);

        var state = stored.Select(JobEventDataEnvelope.Read)
            .FirstOrDefault(item => item.EventType == JobTaskStateEventType);
        if (state.EventType == JobTaskStateEventType)
            task.LoadPersistenceState(state.Data);
        else
            task.MarkPersistenceStatePersisted();
    }
}
