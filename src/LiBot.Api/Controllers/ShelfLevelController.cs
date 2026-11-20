using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.ShelfLevel;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelf-levels")]
public sealed class ShelfLevelController(IShelfService shelves) : ControllerBase
{
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, StatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var current = await shelves.GetLevelByIdAsync(id, cancellationToken);
        if (current is null) return NotFound(new { message = "Shelf level was not found." });
        try
        {
            var updated = await shelves.UpdateLevelAsync(id,
                new ShelfLevelWriteRequest(current.ShelfId, current.LevelNumber, current.Name, request.Status),
                cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }
}
