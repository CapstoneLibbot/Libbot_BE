using Microsoft.AspNetCore.Mvc;
using LiBot.Contracts.Robot;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RobotController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<RobotDto>> GetAll()
    {
        var robots = new[]
        {
            new RobotDto(Guid.NewGuid(), "Robot-01", "SN-001", "Idle", 87.5),
            new RobotDto(Guid.NewGuid(), "Robot-02", "SN-002", "Busy", 62.1)
        };

        return Ok(robots);
    }

    [HttpPost("command")]
    public ActionResult<string> SendCommand([FromBody] string command)
    {
        return Ok($"Command sent: {command}");
    }
}
