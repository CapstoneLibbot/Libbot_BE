using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    [HttpGet]
    public ActionResult<object> GetInventory()
    {
        return Ok(new
        {
            items = new[]
            {
                new { name = "Battery", quantity = 12, unit = "pcs" },
                new { name = "Toolbox", quantity = 4, unit = "set" }
            }
        });
    }
}
