using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Floor;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/floors")]
public class FloorController : ControllerBase
{
    private readonly IFloorService _service;
    public FloorController(IFloorService service) => _service = service ?? throw new ArgumentNullException(nameof(service));

    [HttpGet]
    public async Task<ActionResult<BaseResponse<FloorListResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] string sortBy = "floorNumber", [FromQuery] string sortOrder = "asc",
        [FromQuery] string? expand = null, CancellationToken cancellationToken = default)
    {
        if (!ValidPaging(page, pageSize)) return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "floorNumber", "name", "status" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase)) return BadRequest(Error("sortBy must be floorNumber, name or status."));
        if (!ValidSort(sortOrder)) return BadRequest(Error("sortOrder must be asc or desc."));
        if (!ValidExpand(expand, "aisles")) return BadRequest(Error("expand supports aisles."));
        var response = await _service.GetAllAsync(new FloorQuery(page, pageSize, search, sortBy, sortOrder, expand), cancellationToken);
        return Ok(new BaseResponse<FloorListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<FloorResponse>>> GetById(Guid id, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        if (!ValidExpand(expand, "aisles")) return BadRequest(Error("expand supports aisles."));
        var response = await _service.GetByIdAsync(id, expand, cancellationToken);
        return response is null ? NotFound(Error("Floor was not found.")) : Ok(new BaseResponse<FloorResponse>(response));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<FloorResponse>>> Create([FromBody] FloorWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try { var response = await _service.CreateAsync(request, expand, cancellationToken); return CreatedAtAction(nameof(GetById), new { id = response.Id }, new BaseResponse<FloorResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<FloorResponse>>> Update(Guid id, [FromBody] FloorWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try { var response = await _service.UpdateAsync(id, request, expand, cancellationToken); return response is null ? NotFound(Error("Floor was not found.")) : Ok(new BaseResponse<FloorResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _service.DeleteAsync(id, cancellationToken)) return NotFound(Error("Floor was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Floor deleted successfully."));
    }

    private static bool ValidPaging(int page, int pageSize) => page >= 1 && pageSize is >= 1 and <= 100 && (long)(page - 1) * pageSize <= int.MaxValue;
    private static bool ValidSort(string value) => value.Equals("asc", StringComparison.OrdinalIgnoreCase) || value.Equals("desc", StringComparison.OrdinalIgnoreCase);
    private static bool ValidExpand(string? value, params string[] supported) => string.IsNullOrWhiteSpace(value) || value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(item => supported.Contains(item, StringComparer.OrdinalIgnoreCase));
    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
