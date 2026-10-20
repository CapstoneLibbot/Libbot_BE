using System.Reflection;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Services;

public sealed class ScheduleService(IGenericRepository<Schedule> repository) : IScheduleService
{
    public async Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).OrderBy(schedule => schedule.StartDate).ToList();

    public Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<Schedule> CreateAsync(ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var schedule = CreateSchedule(request);
        repository.Add(schedule);
        await repository.SaveChangesAsync(cancellationToken);
        return schedule;
    }

    public async Task<Schedule?> UpdateAsync(Guid id, ScheduleWriteRequest request, CancellationToken cancellationToken)
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
        return current;
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
        new(request.ScheduleType, request.ScheduleTime, request.StartDate, request.EndDate, request.CreatedBy);
}
