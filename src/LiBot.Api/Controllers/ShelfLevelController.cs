using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.ShelfLevel;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelf-levels")]
public sealed class ShelfLevelController(IShelfService shelves) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<BaseResponse<ShelfLevelListResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? shelfId = null,
        [FromQuery] string sortBy = "levelNumber",
        [FromQuery] string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "levelnumber", "status" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            return BadRequest(Error("sortBy must be levelNumber or status."));
        if (!sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase))
            return BadRequest(Error("sortOrder must be asc or desc."));

        var response = await shelves.GetLevelsAsync(
            new ShelfLevelQuery(page, pageSize, shelfId, sortBy, sortOrder), cancellationToken);
        return Ok(new BaseResponse<ShelfLevelListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var response = await shelves.GetLevelByIdAsync(id, cancellationToken);
        return response is null
            ? NotFound(Error("Shelf level was not found."))
            : Ok(new BaseResponse<ShelfLevelResponse>(response));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> Create(
        [FromBody] ShelfLevelWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await shelves.CreateLevelAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, new BaseResponse<ShelfLevelResponse>(response));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> Update(
        Guid id,
        [FromBody] ShelfLevelWriteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await shelves.UpdateLevelAsync(id, request, cancellationToken);
            return response is null
                ? NotFound(Error("Shelf level was not found."))
                : Ok(new BaseResponse<ShelfLevelResponse>(response));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await shelves.DeleteLevelAsync(id, cancellationToken))
            return NotFound(Error("Shelf level was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Shelf level deleted successfully."));
    }

    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
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
