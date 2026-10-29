using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Endpoint;
using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;
using EndpointGroupDto = LiBot.Application.Models.EndpointGroup.EndpointGroupDto;
using EndpointGroupMemberDto = LiBot.Application.Models.EndpointGroupMember.EndpointGroupMemberDto;

namespace LiBot.Api.Controllers;

/// <summary>CRUD endpoint, nhóm endpoint và quan hệ thành viên trong thư viện.</summary>
[ApiController]
[Route("api/endpoints")]
public class EndpointController : ControllerBase
{
    private readonly IEndpointService _endpointService;
    private readonly ILibraryResourceService<EndpointGroup, EndpointGroupDto> _groupService;
    private readonly ILibraryResourceService<EndpointGroupMember, EndpointGroupMemberDto> _memberService;

    public EndpointController(
        IEndpointService endpointService,
        ILibraryResourceService<EndpointGroup, EndpointGroupDto> groupService,
        ILibraryResourceService<EndpointGroupMember, EndpointGroupMemberDto> memberService)
    {
        _endpointService = endpointService ?? throw new ArgumentNullException(nameof(endpointService));
        _groupService = groupService ?? throw new ArgumentNullException(nameof(groupService));
        _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
    }

    [HttpGet]
    public async Task<ActionResult<BaseResponse<EndpointListResponse>>> GetEndpoints(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] Guid? mapId = null, [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "name", [FromQuery] string sortOrder = "asc",
        [FromQuery] string? expand = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue)
            return BadRequest(Error("page must be at least 1 and pageSize must be between 1 and 100."));
        if (!new[] { "name", "created", "createdatutc" }.Contains(sortBy.ToLowerInvariant()))
            return BadRequest(Error("sortBy must be name or created."));
        if (!new[] { "asc", "desc" }.Contains(sortOrder.ToLowerInvariant()))
            return BadRequest(Error("sortOrder must be asc or desc."));
        if (!HasValidExpansion(expand, "map"))
            return BadRequest(Error("expand supports map."));

        var response = await _endpointService.GetAllAsync(
            new EndpointQuery(page, pageSize, search, mapId, isActive, sortBy, sortOrder, expand), cancellationToken);
        return Ok(new BaseResponse<EndpointListResponse>(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BaseResponse<EndpointResponse>>> GetEndpointById(
        Guid id, [FromQuery] string? expand, CancellationToken cancellationToken)
    {
        if (!HasValidExpansion(expand, "map"))
            return BadRequest(Error("expand supports map."));
        var endpoint = await _endpointService.GetByIdAsync(id, expand, cancellationToken);
        return endpoint is null ? NotFound(Error("Endpoint was not found.")) : Ok(new BaseResponse<EndpointResponse>(endpoint));
    }

    [HttpPost]
    public async Task<ActionResult<BaseResponse<EndpointResponse>>> CreateEndpoint(
        [FromBody] EndpointWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = await _endpointService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetEndpointById), new { id = endpoint.Id },
                new BaseResponse<EndpointResponse>(endpoint));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BaseResponse<EndpointResponse>>> UpdateEndpoint(
        Guid id, [FromBody] EndpointWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var endpoint = await _endpointService.UpdateAsync(id, request, cancellationToken);
            return endpoint is null ? NotFound(Error("Endpoint was not found.")) : Ok(new BaseResponse<EndpointResponse>(endpoint));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
        catch (KeyNotFoundException exception) { return NotFound(Error(exception.Message)); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteEndpoint(Guid id, CancellationToken cancellationToken)
    {
        if (!await _endpointService.DeleteAsync(id, cancellationToken)) return NotFound(Error("Endpoint was not found."));
        return Ok(BaseResponse<object>.SuccessMessage("Endpoint deleted successfully."));
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups(CancellationToken cancellationToken) =>
        Ok(new BaseResponse<IReadOnlyList<EndpointGroupDto>>(await _groupService.GetAllAsync(cancellationToken)));

    [HttpGet("groups/{id:guid}")]
    public async Task<IActionResult> GetGroupById(Guid id, CancellationToken cancellationToken)
    {
        var group = await _groupService.GetByIdAsync(id, cancellationToken);
        return group is null ? NotFound(Error("Endpoint group was not found.")) : Ok(new BaseResponse<EndpointGroupDto>(group));
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] EndpointGroupDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _groupService.CreateAsync(dto, cancellationToken);
            return Created($"/api/endpoints/groups/{group.Id}", new BaseResponse<EndpointGroupDto>(group));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpPut("groups/{id:guid}")]
    public async Task<IActionResult> UpdateGroup(Guid id, [FromBody] EndpointGroupDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var group = await _groupService.UpdateAsync(id, dto, cancellationToken);
            return group is null ? NotFound(Error("Endpoint group was not found.")) : Ok(new BaseResponse<EndpointGroupDto>(group));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpDelete("groups/{id:guid}")]
    public async Task<IActionResult> DeleteGroup(Guid id, CancellationToken cancellationToken) =>
        await _groupService.DeleteAsync(id, cancellationToken) ? Ok(BaseResponse<object>.SuccessMessage("Endpoint group deleted successfully.")) : NotFound(Error("Endpoint group was not found."));

    [HttpGet("group-members")]
    public async Task<IActionResult> GetGroupMembers(CancellationToken cancellationToken) =>
        Ok(new BaseResponse<IReadOnlyList<EndpointGroupMemberDto>>(await _memberService.GetAllAsync(cancellationToken)));

    [HttpGet("group-members/{id:guid}")]
    public async Task<IActionResult> GetGroupMemberById(Guid id, CancellationToken cancellationToken)
    {
        var member = await _memberService.GetByIdAsync(id, cancellationToken);
        return member is null ? NotFound(Error("Endpoint group member was not found.")) : Ok(new BaseResponse<EndpointGroupMemberDto>(member));
    }

    [HttpPost("group-members")]
    public async Task<IActionResult> CreateGroupMember([FromBody] EndpointGroupMemberDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var member = await _memberService.CreateAsync(dto, cancellationToken);
            return Created($"/api/endpoints/group-members/{member.Id}", new BaseResponse<EndpointGroupMemberDto>(member));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpPut("group-members/{id:guid}")]
    public async Task<IActionResult> UpdateGroupMember(Guid id, [FromBody] EndpointGroupMemberDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var member = await _memberService.UpdateAsync(id, dto, cancellationToken);
            return member is null ? NotFound(Error("Endpoint group member was not found.")) : Ok(new BaseResponse<EndpointGroupMemberDto>(member));
        }
        catch (ArgumentException exception) { return BadRequest(Error(exception.Message)); }
    }

    [HttpDelete("group-members/{id:guid}")]
    public async Task<IActionResult> DeleteGroupMember(Guid id, CancellationToken cancellationToken) =>
        await _memberService.DeleteAsync(id, cancellationToken) ? Ok(BaseResponse<object>.SuccessMessage("Endpoint group member deleted successfully.")) : NotFound(Error("Endpoint group member was not found."));

    private static bool HasValidExpansion(string? expand, params string[] supported)
    {
        if (string.IsNullOrWhiteSpace(expand)) return true;
        var values = expand.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return values.All(value => supported.Contains(value, StringComparer.OrdinalIgnoreCase));
    }

    private static BaseResponse<object> Error(string message) => BaseResponse<object>.Failure(message);
}
