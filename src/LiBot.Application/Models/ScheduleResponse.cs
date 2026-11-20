namespace LiBot.Application.Models;

public sealed record ScheduleResponse(
    Guid Id,
    Guid? CreatedBy,
    string Name,
    string? Data,
    string ScheduleType,
    TimeOnly ScheduleTime,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateTime? LastRunAtUtc,
    DateTime? NextRunAtUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
