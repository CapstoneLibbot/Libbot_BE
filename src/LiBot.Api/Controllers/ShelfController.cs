using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Shelf;
using LiBot.Application.Models.ShelfLevel;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelves")]
public sealed class ShelfController(IShelfService service) : ControllerBase
{
    [HttpGet("all")]
    public async Task<IActionResult> GetAllOptions([FromQuery] Guid? aisleId, CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(new ShelfQuery(1, 100, null, aisleId, "name", "asc"), cancellationToken);
        return Ok(new BaseResponse<IReadOnlyList<object>>(result.Items.Select(x => (object)new { x.Id, x.AisleId, x.Name, x.Status }).ToList()));
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<ShelfListResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] Guid? aisleId = null, [FromQuery] string sortBy = "name", [FromQuery] string sortOrder = "asc", [FromQuery] string? expand = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || !new[] { "name", "status" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase) || !ValidSort(sortOrder) || !ValidExpand(expand, "aisle", "levels", "endpoints")) return BadRequest(Error("Invalid paging, sort or expand value."));
        return Ok(new BaseResponse<ShelfListResponse>(await service.GetAllAsync(new ShelfQuery(page, pageSize, search, aisleId, sortBy, sortOrder, expand), cancellationToken)));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfResponse>>> GetById(Guid id, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        if (!ValidExpand(expand, "aisle", "levels", "endpoints")) return BadRequest(Error("expand supports aisle, levels and endpoints."));
        var result = await service.GetByIdAsync(id, expand, cancellationToken);
        return result is null ? NotFound(Error("Shelf was not found.")) : Ok(new BaseResponse<ShelfResponse>(result));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<ShelfResponse>>> Create([FromBody] ShelfWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try { var result = await service.CreateAsync(request, expand, cancellationToken); return CreatedAtAction(nameof(GetById), new { id = result.Id, expand }, new BaseResponse<ShelfResponse>(result)); }
        catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); } catch (KeyNotFoundException ex) { return NotFound(Error(ex.Message)); } catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfResponse>>> Update(Guid id, [FromBody] ShelfWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try { var result = await service.UpdateAsync(id, request, expand, cancellationToken); return result is null ? NotFound(Error("Shelf was not found.")) : Ok(new BaseResponse<ShelfResponse>(result)); }
        catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); } catch (KeyNotFoundException ex) { return NotFound(Error(ex.Message)); } catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    { try { return await service.DeleteAsync(id, cancellationToken) ? Ok(BaseResponse<object>.SuccessMessage("Shelf deleted successfully.")) : NotFound(Error("Shelf was not found.")); } catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); } }

    [HttpGet("{shelfId:guid}/levels")]
    public async Task<ActionResult<BaseResponse<ShelfLevelListResponse>>> GetLevels(Guid shelfId, [FromQuery] string sortOrder = "asc", CancellationToken cancellationToken = default)
    { if (!ValidSort(sortOrder)) return BadRequest(Error("sortOrder must be asc or desc.")); return Ok(new BaseResponse<ShelfLevelListResponse>(await service.GetLevelsAsync(new ShelfLevelQuery(1, 100, shelfId, "levelNumber", sortOrder), cancellationToken))); }

    [HttpGet("/api/shelf-levels/{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> GetLevelById(Guid id, CancellationToken cancellationToken)
    { var result = await service.GetLevelByIdAsync(id, cancellationToken); return result is null ? NotFound(Error("Shelf level was not found.")) : Ok(new BaseResponse<ShelfLevelResponse>(result)); }

    [HttpPost("{shelfId:guid}/levels")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> CreateLevel(Guid shelfId, [FromBody] ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    { if (request.ShelfId != shelfId) return BadRequest(Error("shelfId in route and body must match.")); try { var result = await service.CreateLevelAsync(request, cancellationToken); return CreatedAtAction(nameof(GetLevelById), new { id = result.Id }, new BaseResponse<ShelfLevelResponse>(result)); } catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); } catch (KeyNotFoundException ex) { return NotFound(Error(ex.Message)); } catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); } }

    [HttpPut("/api/shelf-levels/{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> UpdateLevel(Guid id, [FromBody] ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    { try { var result = await service.UpdateLevelAsync(id, request, cancellationToken); return result is null ? NotFound(Error("Shelf level was not found.")) : Ok(new BaseResponse<ShelfLevelResponse>(result)); } catch (ArgumentException ex) { return BadRequest(Error(ex.Message)); } catch (KeyNotFoundException ex) { return NotFound(Error(ex.Message)); } catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); } }

    [HttpDelete("/api/shelf-levels/{id:guid}")]
    public async Task<IActionResult> DeleteLevel(Guid id, CancellationToken cancellationToken)
    { try { return await service.DeleteLevelAsync(id, cancellationToken) ? Ok(BaseResponse<object>.SuccessMessage("Shelf level deleted successfully.")) : NotFound(Error("Shelf level was not found.")); } catch (InvalidOperationException ex) { return Conflict(Error(ex.Message)); } }

    private static bool ValidSort(string value) => value.Equals("asc", StringComparison.OrdinalIgnoreCase) || value.Equals("desc", StringComparison.OrdinalIgnoreCase);
    private static bool ValidExpand(string? value, params string[] allowed) => string.IsNullOrWhiteSpace(value) || value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(x => allowed.Contains(x, StringComparer.OrdinalIgnoreCase));
    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
