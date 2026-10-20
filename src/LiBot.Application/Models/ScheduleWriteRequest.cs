
namespace LiBot.Application.Models;

public record ScheduleWriteRequest(string ScheduleType, TimeOnly ScheduleTime, DateOnly StartDate,
    DateOnly? EndDate = null, Guid? CreatedBy = null);
