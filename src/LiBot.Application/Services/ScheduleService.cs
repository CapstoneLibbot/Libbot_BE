using System.Reflection;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Services;

public sealed class ScheduleService(IGenericRepository<Schedule> repository) : IScheduleService
{
    public async Task<IReadOnlyList<ScheduleResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).OrderBy(schedule => schedule.StartDate).Select(ToResponse).ToList();

    public async Task<ScheduleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByIdAsync(id, cancellationToken);
        return schedule is null ? null : ToResponse(schedule);
    }

    public async Task<ScheduleResponse> CreateAsync(ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var schedule = CreateSchedule(request);
        repository.Add(schedule);
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(schedule);
    }

    public async Task<ScheduleResponse?> UpdateAsync(Guid id, ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null) return null;
        var replacement = CreateSchedule(request);
        foreach (var property in typeof(Schedule).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name is "Id" or "CreatedAtUtc" or "UpdatedAtUtc" || property.GetSetMethod(true) is null) continue;
            property.SetValue(current, property.GetValue(replacement));
        }
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(current);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetByIdAsync(id, cancellationToken);
        if (schedule is null) return false;
        repository.Remove(schedule);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Schedule CreateSchedule(ScheduleWriteRequest request) =>
        new(request.Name, request.Data, request.ScheduleType, request.ScheduleTime, request.StartDate,
            request.EndDate, request.CreatedBy);

    private static ScheduleResponse ToResponse(Schedule schedule) => new(
        schedule.Id, schedule.CreatedBy, schedule.Name, schedule.Data, schedule.ScheduleType, schedule.ScheduleTime,
        schedule.StartDate, schedule.EndDate, schedule.LastRunAtUtc, schedule.NextRunAtUtc,
        schedule.CreatedAtUtc, schedule.UpdatedAtUtc);
}
