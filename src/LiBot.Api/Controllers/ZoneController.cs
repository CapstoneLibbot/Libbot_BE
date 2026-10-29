using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Zone;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/zones")]
public class ZoneController : ControllerBase
{
    private readonly IZoneService _service;
    public ZoneController(IZoneService service) => _service = service ?? throw new ArgumentNullException(nameof(service));

    [HttpGet]
    public async Task<ActionResult<BaseResponse<ZoneListResponse>>> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] Guid? mapId = null, [FromQuery] string? zoneType = null, [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "zoneType", [FromQuery] string sortOrder = "asc", [FromQuery] string? expand = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue) return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "zoneType", "maxSpeed", "created", "createdAtUtc" }.Contains(sortBy, StringComparer.OrdinalIgnoreCase)) return BadRequest(Error("sortBy must be zoneType, maxSpeed or created."));
        if (!new[] { "asc", "desc" }.Contains(sortOrder, StringComparer.OrdinalIgnoreCase)) return BadRequest(Error("sortOrder must be asc or desc."));
        if (!ValidExpand(expand, "map")) return BadRequest(Error("expand supports map."));
        var response = await _service.GetAllAsync(new ZoneQuery(page, pageSize, search, mapId, zoneType, isActive, sortBy, sortOrder, expand), cancellationToken);
        return Ok(new BaseResponse<ZoneListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ZoneResponse>>> GetById(Guid id, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        if (!ValidExpand(expand, "map")) return BadRequest(Error("expand supports map."));
        var response = await _service.GetByIdAsync(id, expand, cancellationToken);
        return response is null ? NotFound(Error("Zone was not found.")) : Ok(new BaseResponse<ZoneResponse>(response));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<ZoneResponse>>> Create([FromBody] ZoneWriteRequest request, CancellationToken cancellationToken)
    {
        try { var response = await _service.CreateAsync(request, cancellationToken); return CreatedAtAction(nameof(GetById), new { id = response.Id }, new BaseResponse<ZoneResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<ZoneResponse>>> Update(Guid id, [FromBody] ZoneWriteRequest request, CancellationToken cancellationToken)
    {
        try { var response = await _service.UpdateAsync(id, request, cancellationToken); return response is null ? NotFound(Error("Zone was not found.")) : Ok(new BaseResponse<ZoneResponse>(response)); }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _service.DeleteAsync(id, cancellationToken)) return NotFound(Error("Zone was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Zone deleted successfully."));
    }

    private static bool ValidExpand(string? value, params string[] supported) => string.IsNullOrWhiteSpace(value) || value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(item => supported.Contains(item, StringComparer.OrdinalIgnoreCase));
    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
