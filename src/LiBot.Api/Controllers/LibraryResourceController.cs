using System.Reflection;
using System.Text.Json;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

public abstract class LibraryResourceController<TEntity>(LiBotDbContext db) : ControllerBase where TEntity : class
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await db.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Set<TEntity>().FindAsync([id], cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var entity = Deserialize(body);
        if (entity is null) return BadRequest("Invalid request body.");
        db.Set<TEntity>().Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ReadId(entity) }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var current = await db.Set<TEntity>().FindAsync([id], cancellationToken);
        if (current is null) return NotFound();
        var replacement = Deserialize(body);
        if (replacement is null) return BadRequest("Invalid request body.");
        foreach (var property in typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name is "Id" or "CreatedAtUtc" or "UpdatedAtUtc" || property.GetSetMethod(true) is null) continue;
            property.SetValue(current, property.GetValue(replacement));
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(current);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Set<TEntity>().FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();
        db.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static TEntity? Deserialize(JsonElement body)
    {
        try { return body.Deserialize<TEntity>(JsonOptions); }
        catch (JsonException) { return null; }
    }

    private static object? ReadId(TEntity entity) => typeof(TEntity).GetProperty("Id")?.GetValue(entity);
}
