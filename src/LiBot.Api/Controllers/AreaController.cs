using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Area;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD các khu vực chức năng trong thư viện.</summary>
[ApiController]
[Route("api/areas")]
public class AreaController : ControllerBase
{
    private readonly IAreaService _areaService;

    public AreaController(IAreaService service)
    {
        _areaService = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<AreaListResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? floorId = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? expand = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "name", "created", "createdatutc" }.Contains(sortBy.ToLowerInvariant()))
            return BadRequest(Error("sortBy must be name or created."));
        if (!new[] { "asc", "desc" }.Contains(sortOrder.ToLowerInvariant()))
            return BadRequest(Error("sortOrder must be asc or desc."));

        var response = await _areaService.GetAllAsync(
            new AreaQuery(page, pageSize, search, floorId, sortBy, sortOrder, expand),
            cancellationToken);
        return Ok(new BaseResponse<AreaListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<AreaResponse>>> GetById(
        Guid id,
        [FromQuery] string? expand,
        CancellationToken cancellationToken)
    {
        var response = await _areaService.GetByIdAsync(id, expand, cancellationToken);
        return response is null ? NotFound(Error("Area was not found.")) : Ok(new BaseResponse<AreaResponse>(response));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<AreaResponse>>> Create(
        [FromBody] AreaWriteRequest request,
        [FromQuery] string? expand,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _areaService.CreateAsync(request, expand, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id, expand },
                new BaseResponse<AreaResponse>(response));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<AreaResponse>>> Update(
        Guid id,
        [FromBody] AreaWriteRequest request,
        [FromQuery] string? expand,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _areaService.UpdateAsync(id, request, expand, cancellationToken);
            return response is null ? NotFound(Error("Area was not found.")) : Ok(new BaseResponse<AreaResponse>(response));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _areaService.DeleteAsync(id, cancellationToken))
            return NotFound(Error("Area was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Area deleted successfully."));
    }

    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
