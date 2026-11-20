using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Endpoint;
using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;
using EndpointGroupDto = LiBot.Application.Models.EndpointGroup.EndpointGroupDto;
using EndpointGroupMemberDto = LiBot.Application.Models.EndpointGroupMember.EndpointGroupMemberDto;

namespace LiBot.Api.Controllers;

/// <summary>CRUD endpoint, nhóm endpoint và quan hệ thành viên trong thư viện.</summary>
[ApiController]
[Route("api/endpoints")]
public sealed class EndpointController(
    ILibraryResourceService<LibraryEndpoint, EndpointDto> service,
    ILibraryMapService maps) : LibraryResourceController<LibraryEndpoint, EndpointDto>(service)
{
    [HttpPost]
    public override async Task<IActionResult> Create([FromBody] EndpointDto dto, CancellationToken cancellationToken)
    {
        try { _ = dto.ToEntity(); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        var map = await maps.GetByIdAsync(dto.MapId, cancellationToken);
        if (map is null)
            return NotFound(new { message = "Map was not found." });
        if (!MapBoundsValidator.Contains(map, dto.PositionX, dto.PositionY))
            return BadRequest(new { message = "Endpoint coordinates must be inside the selected map bounds." });
        return await base.Create(dto, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public override async Task<IActionResult> Update(Guid id, EndpointDto dto, CancellationToken cancellationToken)
    {
        try { _ = dto.ToEntity(); }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
        var map = await maps.GetByIdAsync(dto.MapId, cancellationToken);
        if (map is null)
            return NotFound(new { message = "Map was not found." });
        if (!MapBoundsValidator.Contains(map, dto.PositionX, dto.PositionY))
            return BadRequest(new { message = "Endpoint coordinates must be inside the selected map bounds." });
        return await base.Update(id, dto, cancellationToken);
    }
}
