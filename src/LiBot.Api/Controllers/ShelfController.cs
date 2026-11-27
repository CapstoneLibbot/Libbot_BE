using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/shelves")]
public sealed class ShelfController(
    ILibraryResourceService<Shelf, ShelfDto> service,
    ILibraryResourceService<LibraryEndpoint, EndpointDto> endpoints)
    : LibraryResourceController<Shelf, ShelfDto>(service)
{
    [HttpPost]
    public override async Task<IActionResult> Create(ShelfDto dto, CancellationToken cancellationToken)
    {
        if (await endpoints.GetByIdAsync(dto.StartEndpointId, cancellationToken) is null
            || await endpoints.GetByIdAsync(dto.EndEndpointId, cancellationToken) is null)
            return BadRequest(new { message = "Endpoint was not found." });

        return await base.Create(dto, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public override async Task<IActionResult> Update(Guid id, ShelfDto dto, CancellationToken cancellationToken)
    {
        if (await endpoints.GetByIdAsync(dto.StartEndpointId, cancellationToken) is null
            || await endpoints.GetByIdAsync(dto.EndEndpointId, cancellationToken) is null)
            return BadRequest(new { message = "Endpoint was not found." });

        return await base.Update(id, dto, cancellationToken);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id, StatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var current = await ResourceService.GetByIdAsync(id, cancellationToken);
        if (current is null) return NotFound(new { message = "Shelf was not found." });
        current.Status = request.Status;
        try
        {
            var updated = await ResourceService.UpdateAsync(id, current, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException exception) { return BadRequest(new { message = exception.Message }); }
    }
}
