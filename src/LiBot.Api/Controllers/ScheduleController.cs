using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/workflows/schedules")]
public class ScheduleController(IScheduleService schedules) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await schedules.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var schedule = await schedules.GetByIdAsync(id, cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var schedule = await schedules.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ScheduleWriteRequest request, CancellationToken cancellationToken)
    {
        var schedule = await schedules.UpdateAsync(id, request, cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await schedules.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
