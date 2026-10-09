using System.Text.Json;
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
        return Ok(maps.Select(x => new MapResponse(x.Id, x.Name, x.Width, x.Height, ParseData(x.MapData))));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var map = await db.Maps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return map is null ? NotFound() : Ok(new MapResponse(map.Id, map.Name, map.Width, map.Height, ParseData(map.MapData)));
    }

    private static JsonElement? ParseData(string value)
    {
        try { return JsonDocument.Parse(value).RootElement.Clone(); }
        catch (JsonException) { return null; }
    }
}

public record MapResponse(Guid Id, string Name, int Width, int Height, JsonElement? Data);
