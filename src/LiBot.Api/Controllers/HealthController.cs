using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Endpoint kiểm tra API đang hoạt động hay không.</summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>Trả trạng thái service và thời điểm phản hồi.</summary>
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
