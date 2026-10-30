using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.Shelf;
using LiBot.Application.Models.ShelfLevel;
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
        if (dto.EndpointId is Guid endpointId
            && await endpoints.GetByIdAsync(endpointId, cancellationToken) is null)
            return BadRequest(new { message = "Endpoint was not found." });

        return await base.Create(dto, cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public override async Task<IActionResult> Update(Guid id, ShelfDto dto, CancellationToken cancellationToken)
    {
        if (dto.EndpointId is Guid endpointId
            && await endpoints.GetByIdAsync(endpointId, cancellationToken) is null)
            return BadRequest(new { message = "Endpoint was not found." });

        return await base.Update(id, dto, cancellationToken);
    }
}
