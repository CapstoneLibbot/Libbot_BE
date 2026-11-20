using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using LiBot.Domain.Library;
using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>CRUD zones and keep the active robot navigation filters in sync.</summary>
[ApiController]
[Route("api/zones")]
public sealed class ZoneController(
    ILibraryResourceService<Zone, ZoneDto> service,
    ILibraryMapService maps,
    IRobotRuntimeService runtimes,
    ILogger<ZoneController> logger) : LibraryResourceController<Zone, ZoneDto>(service)
{
    [HttpPost]
    public override async Task<IActionResult> Create(ZoneDto dto, CancellationToken cancellationToken)
    {
        if (!TryValidateZone(dto, out var validationError))
            return BadRequest(new { message = validationError });

        var map = await maps.GetByIdAsync(dto.MapId, cancellationToken);
        if (map is null)
            return NotFound(new { message = "Map was not found." });
        if (!MapBoundsValidator.Contains(map, dto.MinX, dto.MinY, dto.MaxX, dto.MaxY))
            return BadRequest(new { message = "Zone bounds must be inside the selected map bounds." });

        var result = await base.Create(dto, cancellationToken);
        if (result is ObjectResult { StatusCode: StatusCodes.Status201Created })
            await RefreshActiveRuntimeZones(dto.MapId, cancellationToken);
        return result;
    }

    [HttpPut("{id:guid}")]
    public override async Task<IActionResult> Update(Guid id, ZoneDto dto, CancellationToken cancellationToken)
    {
        if (!TryValidateZone(dto, out var validationError))
            return BadRequest(new { message = validationError });

        var map = await maps.GetByIdAsync(dto.MapId, cancellationToken);
        if (map is null)
            return NotFound(new { message = "Map was not found." });
        if (!MapBoundsValidator.Contains(map, dto.MinX, dto.MinY, dto.MaxX, dto.MaxY))
            return BadRequest(new { message = "Zone bounds must be inside the selected map bounds." });

        var previous = await ResourceService.GetByIdAsync(id, cancellationToken);
        var result = await base.Update(id, dto, cancellationToken);
        if (result is OkObjectResult)
        {
            if (previous is not null && previous.MapId != dto.MapId)
                await RefreshActiveRuntimeZones(previous.MapId, cancellationToken);
            await RefreshActiveRuntimeZones(dto.MapId, cancellationToken);
        }
        return result;
    }

    [HttpDelete("{id:guid}")]
    public override async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var previous = await ResourceService.GetByIdAsync(id, cancellationToken);
        var result = await base.Delete(id, cancellationToken);
        if (result is NoContentResult && previous is not null)
            await RefreshActiveRuntimeZones(previous.MapId, cancellationToken);
        return result;
    }

    private static bool TryValidateZone(ZoneDto dto, out string? error)
    {
        try
        {
            _ = dto.ToEntity();
            error = null;
            return true;
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private async Task RefreshActiveRuntimeZones(Guid mapId, CancellationToken cancellationToken)
    {
        var policies = (await ResourceService.GetAllAsync(cancellationToken))
            .Where(zone => zone.MapId == mapId && zone.IsActive)
            .Select(zone => new NavigationZonePolicy(
                zone.ZoneType, zone.MinX, zone.MinY, zone.MaxX, zone.MaxY, zone.MaxSpeed))
            .ToList();

        foreach (var runtime in runtimes.GetAll().Where(runtime =>
                     runtime.NavigationActive
                     && Guid.TryParse(runtime.Telemetry?.ActiveMapId, out var activeMapId)
                     && activeMapId == mapId))
        {
            var result = await runtimes.UpdateNavigationZonesAsync(
                runtime.Id, mapId, policies, cancellationToken);
            if (!result.Success)
                logger.LogWarning(
                    "Zone changes for map {MapId} were saved but not applied to active runtime {RuntimeId}: {Error}",
                    mapId, runtime.Id, result.Error);
        }
    }
}
