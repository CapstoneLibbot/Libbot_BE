
namespace LiBot.Application.Models;

public record ScheduleWriteRequest(string Name, string ScheduleType, TimeOnly ScheduleTime, DateOnly StartDate,
    DateOnly? EndDate = null, Guid? CreatedBy = null, string? Data = null);
