using System.Text.Json;
using LiBot.Api.Models;
using LiBot.Api.Services;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/maps")]
public class MapController(LiBotDbContext db, RobotRuntimeService runtimes) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var maps = await db.Maps.AsNoTracking()
            .Where(map => !EF.Functions.Like(map.MapData, "%occupancy-grid-v1%"))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
        return Ok(maps.Select(ToResponse));
    }

    [HttpGet("/api/robot-runtimes/real/maps")]
    public async Task<IActionResult> GetRobotMaps(CancellationToken cancellationToken)
    {
        var maps = await db.Maps.AsNoTracking()
            .Where(map => EF.Functions.Like(map.MapData, "%occupancy-grid-v1%"))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var result = new List<SavedRobotMapResponse>();
        foreach (var map in maps)
        {
            try
            {
                using var data = JsonDocument.Parse(map.MapData);
                if (data.RootElement.GetProperty("format").GetString() == "occupancy-grid-v1")
                    result.Add(new SavedRobotMapResponse(map.Id, map.Name, map.Width, map.Height, map.Version));
            }
            catch (JsonException)
            {
                // Ignore maps in the legacy floorplan format.
            }
            catch (KeyNotFoundException) { }
            catch (InvalidOperationException) { }
        }
        return Ok(result);
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

    [HttpPost("/api/robot-runtimes/real/maps")]
    public async Task<IActionResult> SaveRobotSnapshot(SaveRobotMapRequest request, [FromQuery] string? armLeaseId, CancellationToken cancellationToken)
    {
        var grid = runtimes.GetLatestMap("real");
        if (grid is null) return Conflict(new { message = "No live map is available yet." });
        if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { message = "Map name is required." });

        var id = Guid.NewGuid();
        var fileStem = $"/ros2_ws/maps/{id:N}";
        var saved = await runtimes.SaveMapOnRobotAsync("real", fileStem, armLeaseId, cancellationToken);
        if (!saved.Success) return Conflict(new { message = saved.Error });
        var savedGrid = saved.Payload is { } payload && payload.TryGetProperty("map", out var mapPayload)
            ? mapPayload.Deserialize<OccupancyGridDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            : null;
        if (savedGrid is null) return Conflict(new { message = "Robot wrote its map files but did not return the saved grid snapshot." });

        var priorVersions = await db.Maps.AsNoTracking().CountAsync(x => x.Name == request.Name.Trim(), cancellationToken);
        var version = (priorVersions + 1).ToString();
        var data = JsonSerializer.Serialize(new
        {
            format = "occupancy-grid-v1",
            resolution = savedGrid.Resolution,
            origin = new[] { savedGrid.OriginX, savedGrid.OriginY, savedGrid.OriginYaw },
            timestamp = savedGrid.Timestamp,
            cells = savedGrid.Data,
            robotPath = $"{fileStem}.yaml",
        });
        var map = new LibraryMap(request.Name.Trim(), version, checked((int)savedGrid.Width), checked((int)savedGrid.Height), data);
        db.Maps.Add(map);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            return Problem($"Robot files were saved, but the database save failed: {exception.Message}");
        }
        return Ok(new { map.Id, map.Name, map.Width, map.Height, map.Version, data = new { format = "occupancy-grid-v1" } });
    }

    [HttpPost("{id:guid}/start-navigation")]
    public async Task<IActionResult> StartNavigation(Guid id, [FromQuery] string? armLeaseId, CancellationToken cancellationToken)
    {
        var map = await db.Maps.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (map is null) return NotFound();
        try
        {
            using var json = JsonDocument.Parse(map.MapData);
            var path = json.RootElement.GetProperty("robotPath").GetString();
            if (string.IsNullOrWhiteSpace(path)) return Conflict(new { message = "Map has no robot-side PGM/YAML files." });
            var result = await runtimes.StartNavigationModeAsync("real", path, armLeaseId, cancellationToken);
            return result.Success ? Accepted(result.Payload) : Conflict(new { message = result.Error });
        }
        catch (JsonException)
        {
            return Conflict(new { message = "Map data is not a saved robot occupancy map." });
        }
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
        new(map.Id, map.Name, map.Width, map.Height, ParseData(map.MapData), map.Version);

    private static JsonElement? ParseData(string value)
    {
        try { return JsonDocument.Parse(value).RootElement.Clone(); }
        catch (JsonException) { return null; }
    }
}

public record MapResponse(Guid Id, string Name, int Width, int Height, JsonElement? Data, string Version);
public record SavedRobotMapResponse(Guid Id, string Name, int Width, int Height, string Version, object Data)
{
    public SavedRobotMapResponse(Guid id, string name, int width, int height, string version)
        : this(id, name, width, height, version, new { format = "occupancy-grid-v1" }) { }
}
public record MapWriteRequest(string Name, int Width, int Height, JsonElement? Data, string? Version = null);
