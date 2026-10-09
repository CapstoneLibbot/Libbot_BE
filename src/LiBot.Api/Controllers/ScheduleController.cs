using System.Reflection;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflows/schedules")]
public class ScheduleController(LiBotDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.Schedules.AsNoTracking().OrderBy(x => x.StartDate).ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var schedule = new Schedule(request.ScheduleType, request.ScheduleTime, request.StartDate, request.EndDate, request.CreatedBy);
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (schedule is null) return NotFound();
        var replacement = new Schedule(request.ScheduleType, request.ScheduleTime, request.StartDate, request.EndDate, request.CreatedBy);
        foreach (var property in typeof(Schedule).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name is "Id" or "CreatedAtUtc" or "UpdatedAtUtc" || property.GetSetMethod(true) is null) continue;
            property.SetValue(schedule, property.GetValue(replacement));
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(schedule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (schedule is null) return NotFound();
        db.Schedules.Remove(schedule);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record ScheduleWriteRequest(string ScheduleType, TimeOnly ScheduleTime, DateOnly StartDate,
    DateOnly? EndDate = null, Guid? CreatedBy = null);
