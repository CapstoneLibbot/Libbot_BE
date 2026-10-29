using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Aisle;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD lối đi trong thư viện và điểm điều hướng gắn với lối đi.</summary>
[ApiController]
[Route("api/aisles")]
public class AisleController : ControllerBase
{
    private readonly IAisleService _aisleService;

    public AisleController(IAisleService service)
    {
        _aisleService = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<AisleListResponse>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? floorId = null,
        [FromQuery] Guid? endpointId = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? expand = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "name", "code", "created", "createdatutc" }.Contains(sortBy.ToLowerInvariant()))
            return BadRequest(Error("sortBy must be name, code or created."));
        if (!new[] { "asc", "desc" }.Contains(sortOrder.ToLowerInvariant()))
            return BadRequest(Error("sortOrder must be asc or desc."));
        var response = await _aisleService.GetAllAsync(
            new AisleQuery(page, pageSize, search, floorId, endpointId, sortBy, sortOrder, expand),
            cancellationToken);
        return Ok(new BaseResponse<AisleListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<AisleResponse>>> GetById(Guid id, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        var response = await _aisleService.GetByIdAsync(id, expand, cancellationToken);
        return response is null ? NotFound(Error("Aisle was not found.")) : Ok(new BaseResponse<AisleResponse>(response));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<AisleResponse>>> Create([FromBody] AisleWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aisleService.CreateAsync(request, expand, cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = response.Id, expand },
                new BaseResponse<AisleResponse>(response));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<AisleResponse>>> Update(Guid id, [FromBody] AisleWriteRequest request, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _aisleService.UpdateAsync(id, request, expand, cancellationToken);
            return response is null ? NotFound(Error("Aisle was not found.")) : Ok(new BaseResponse<AisleResponse>(response));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!await _aisleService.DeleteAsync(id, cancellationToken))
            return NotFound(Error("Aisle was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Aisle deleted successfully."));
    }

    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
