using System.Text.Json;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/maps")]
public class MapController(LiBotDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var maps = await db.Maps.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return Ok(maps.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var map = await db.Maps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return map is null ? NotFound() : Ok(ToResponse(map));
    }

    [HttpPost]
    public async Task<IActionResult> Create(MapWriteRequest request, CancellationToken cancellationToken)
    {
        var map = new LibraryMap(request.Name, request.Version ?? string.Empty, request.Width, request.Height,
            request.Data?.GetRawText() ?? "{}");
        db.Maps.Add(map);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = map.Id }, ToResponse(map));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MapWriteRequest request, CancellationToken cancellationToken)
    {
        var map = await db.Maps.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (map is null) return NotFound();
        map.Update(request.Name, request.Version ?? map.Version, request.Width, request.Height,
            request.Data?.GetRawText() ?? map.MapData);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(map));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var map = await db.Maps.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (map is null) return NotFound();
        db.Maps.Remove(map);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static MapResponse ToResponse(LibraryMap map) =>
        new(map.Id, map.Name, map.Width, map.Height, ParseData(map.MapData));

    private static JsonElement? ParseData(string value)
    {
        try { return JsonDocument.Parse(value).RootElement.Clone(); }
        catch (JsonException) { return null; }
    }
}

public record MapResponse(Guid Id, string Name, int Width, int Height, JsonElement? Data);
public record MapWriteRequest(string Name, int Width, int Height, JsonElement? Data, string? Version = null);
