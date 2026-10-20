using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

public abstract class LibraryResourceController<TEntity>(ILibraryResourceService<TEntity> service) : ControllerBase where TEntity : class
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await service.GetByIdAsync(id, cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var entity = Deserialize(body);
        if (entity is null) return BadRequest("Invalid request body.");
        await service.CreateAsync(entity, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ReadId(entity) }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var replacement = Deserialize(body);
        if (replacement is null)
            return await service.ExistsAsync(id, cancellationToken) ? BadRequest("Invalid request body.") : NotFound();
        var updated = await service.UpdateAsync(id, replacement, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }

    private static TEntity? Deserialize(JsonElement body)
    {
        try { return body.Deserialize<TEntity>(JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static object? ReadId(TEntity entity) => typeof(TEntity).GetProperty("Id")?.GetValue(entity);
}
