using Microsoft.AspNetCore.Mvc;
using LiBot.Contracts.Mission;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public ActionResult<MissionDto> GetById(Guid id)
    {
        var mission = new MissionDto(
            id,
            "Mission sample",
            "Example mission description",
            "Pending",
            DateTime.UtcNow);

        return Ok(mission);
    }

    [HttpPost]
    public ActionResult<MissionDto> Create([FromBody] MissionDto mission)
    {
        return CreatedAtAction(nameof(GetById), new { id = mission.Id }, mission);
    }
}
