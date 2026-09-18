using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            service = "LiBot.Api",
            timestampUtc = DateTime.UtcNow
        });
    }
}
