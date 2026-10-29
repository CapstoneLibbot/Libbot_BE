using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Shelf;
using LiBot.Application.Models.ShelfLevel;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelves")]
public class ShelfController : ControllerBase
{
    private readonly IShelfService _service;
    public ShelfController(IShelfService service) => _service = service ?? throw new ArgumentNullException(nameof(service));

    [HttpGet]
    public async Task<ActionResult<BaseResponse<ShelfListResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] Guid? aisleId = null, [FromQuery] string sortBy = "name", [FromQuery] string sortOrder = "asc",
        [FromQuery] string? expand = null, CancellationToken cancellationToken = default)
    {
        if (!ValidPaging(page, pageSize)) return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "name", "code", "created", "createdAtUtc" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase)) return BadRequest(Error("sortBy must be name, code or created."));
        if (!ValidSort(sortOrder)) return BadRequest(Error("sortOrder must be asc or desc."));
        if (!ValidExpand(expand, "aisle", "levels")) return BadRequest(Error("expand supports aisle and levels."));
        var response = await _service.GetAllAsync(new ShelfQuery(page, pageSize, search, aisleId, sortBy, sortOrder, expand), cancellationToken);
        return Ok(new BaseResponse<ShelfListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfResponse>>> GetById(Guid id, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        if (!ValidExpand(expand, "aisle", "levels")) return BadRequest(Error("expand supports aisle and levels."));
        var response = await _service.GetByIdAsync(id, expand, cancellationToken);
        return response is null ? NotFound(Error("Shelf was not found.")) : Ok(new BaseResponse<ShelfResponse>(response));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<ShelfResponse>>> Create([FromBody] ShelfWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try { var response = await _service.CreateAsync(request, expand, cancellationToken); return CreatedAtAction(nameof(GetById), new { id = response.Id }, new BaseResponse<ShelfResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfResponse>>> Update(Guid id, [FromBody] ShelfWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try { var response = await _service.UpdateAsync(id, request, expand, cancellationToken); return response is null ? NotFound(Error("Shelf was not found.")) : Ok(new BaseResponse<ShelfResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _service.DeleteAsync(id, cancellationToken)) return NotFound(Error("Shelf was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Shelf deleted successfully."));
    }

    [HttpGet("levels")]
    public async Task<ActionResult<BaseResponse<ShelfLevelListResponse>>> GetLevels(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] Guid? shelfId = null,
        [FromQuery] string sortBy = "levelNumber", [FromQuery] string sortOrder = "asc", CancellationToken cancellationToken = default)
    {
        if (!ValidPaging(page, pageSize)) return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "levelNumber", "created", "createdAtUtc" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase)) return BadRequest(Error("sortBy must be levelNumber or created."));
        if (!ValidSort(sortOrder)) return BadRequest(Error("sortOrder must be asc or desc."));
        return Ok(new BaseResponse<ShelfLevelListResponse>(await _service.GetLevelsAsync(new ShelfLevelQuery(page, pageSize, shelfId, sortBy, sortOrder), cancellationToken)));
    }

    [HttpGet("levels/{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> GetLevelById(Guid id, CancellationToken cancellationToken)
    {
        var response = await _service.GetLevelByIdAsync(id, cancellationToken);
        return response is null ? NotFound(Error("Shelf level was not found.")) : Ok(new BaseResponse<ShelfLevelResponse>(response));
    }

    [HttpPost("levels")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> CreateLevel([FromBody] ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        try { var response = await _service.CreateLevelAsync(request, cancellationToken); return Created($"/api/shelves/levels/{response.Id}", new BaseResponse<ShelfLevelResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("levels/{id:guid}")]
    public async Task<ActionResult<BaseResponse<ShelfLevelResponse>>> UpdateLevel(Guid id, [FromBody] ShelfLevelWriteRequest request, CancellationToken cancellationToken)
    {
        try { var response = await _service.UpdateLevelAsync(id, request, cancellationToken); return response is null ? NotFound(Error("Shelf level was not found.")) : Ok(new BaseResponse<ShelfLevelResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("levels/{id:guid}")]
    public async Task<IActionResult> DeleteLevel(Guid id, CancellationToken cancellationToken)
    {
        if (!await _service.DeleteLevelAsync(id, cancellationToken)) return NotFound(Error("Shelf level was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Shelf level deleted successfully."));
    }

    private static bool ValidPaging(int page, int pageSize) => page >= 1 && pageSize is >= 1 and <= 100 && (long)(page - 1) * pageSize <= int.MaxValue;
    private static bool ValidSort(string value) => value.Equals("asc", StringComparison.OrdinalIgnoreCase) || value.Equals("desc", StringComparison.OrdinalIgnoreCase);
    private static bool ValidExpand(string? value, params string[] supported) => string.IsNullOrWhiteSpace(value) || value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(item => supported.Contains(item, StringComparer.OrdinalIgnoreCase));
    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
