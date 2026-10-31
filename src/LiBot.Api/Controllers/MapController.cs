using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Robot;
using LiBot.Domain.Library;
using LiBot.Domain.Library;
using Microsoft.AspNetCore.Mvc;
using LibraryEndpoint = LiBot.Domain.Library.Endpoint;

namespace LiBot.Api.Controllers;

/// <summary>Quản lý bản đồ thư viện và bản đồ occupancy do robot lưu lại.</summary>
/// <remarks>Dùng <c>/api/robot-runtimes/real/maps</c> để liệt kê và lưu snapshot occupancy của robot.</remarks>
[ApiController]
[Route("api/maps")]
public class MapController(
    ILibraryMapService maps,
    ILibraryResourceService<Zone, ZoneDto> zones,
    IRobotRuntimeService runtimes,
    ILogger<MapController> logger) : ControllerBase
{
    /// <summary>Lấy các bản đồ occupancy robot thật đã lưu.</summary>
    [HttpGet("/api/robot-runtimes/real/maps")]
    public async Task<IActionResult> GetRobotMaps(CancellationToken cancellationToken) =>
        Ok(await maps.GetRobotMapsAsync(cancellationToken));

    /// <summary>Lấy snapshot occupancy mới nhất của robot thật.</summary>
    [HttpGet("/api/robot-runtimes/real/maps/latest")]
    public async Task<IActionResult> GetLatestRobotMap(CancellationToken cancellationToken) =>
        Ok(await maps.GetLatestRobotMapAsync(cancellationToken));

    /// <summary>Lấy chi tiết bản đồ theo ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var map = await maps.GetByIdAsync(id, cancellationToken);
        return map is null ? NotFound() : Ok(map);
    }

    /// <summary>Gets the endpoints assigned to a library map.</summary>
    [HttpGet("{id:guid}/endpoints")]
    public async Task<IActionResult> GetEndpoints(Guid id, CancellationToken cancellationToken)
    {
        if (await maps.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(new { message = "Map was not found." });
        return Ok(await maps.GetEndpointsByMapIdAsync(id, cancellationToken));
    }

    /// <summary>Gets the zones assigned to a library map.</summary>
    [HttpGet("{id:guid}/zones")]
    public async Task<IActionResult> GetZones(Guid id, CancellationToken cancellationToken)
    {
        if (await maps.GetByIdAsync(id, cancellationToken) is null)
            return NotFound(new { message = "Map was not found." });
        return Ok(await maps.GetZonesByMapIdAsync(id, cancellationToken));
    }

    /// <summary>Tạo bản đồ thư viện mới.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(MapWriteRequest request, CancellationToken cancellationToken)
    {
        var map = await maps.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = map.Id }, map);
    }

    /// <summary>Lưu snapshot occupancy và đường dẫn file map trên robot.</summary>
    [HttpPost("/api/robot-runtimes/real/maps")]
    public async Task<IActionResult> SaveRobotSnapshot(
        SaveRobotMapRequest request,
        [FromQuery] string? armLeaseId,
        CancellationToken cancellationToken)
    {
        if (runtimes.GetLatestMap("real") is null)
            return Conflict(new { message = "No live map is available yet." });
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Map name is required." });

        var mapId = Guid.NewGuid();
        var fileStem = $"/ros2_ws/maps/{mapId:N}";
        var saved = await runtimes.SaveMapOnRobotAsync("real", fileStem, armLeaseId, cancellationToken);
        if (!saved.Success) return Conflict(new { message = saved.Error });

        var savedGrid = saved.Payload is { } payload && payload.TryGetProperty("map", out var mapPayload)
            ? mapPayload.Deserialize<OccupancyGridDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web))
            : null;
        if (savedGrid is null)
            return Conflict(new { message = "Robot wrote its map files but did not return the saved grid snapshot." });

        try
        {
            var map = await maps.SaveRobotSnapshotAsync(request.Name, savedGrid, $"{fileStem}.yaml", cancellationToken);
            return Ok(new { map.Id, map.Name, map.Width, map.Height, map.Version, data = new { format = "occupancy-grid-v1" } });
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Robot map files were saved, but map {MapId} ({MapName}) could not be persisted to the database.",
                mapId, request.Name.Trim());
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Robot files were saved, but saving the map to the database failed.",
                detail: exception.GetBaseException().Message);
        }
    }

    /// <summary>Nạp map đã lưu lên robot thật để bắt đầu chế độ điều hướng.</summary>
    [HttpPost("{id:guid}/start-navigation")]
    public async Task<IActionResult> StartNavigation(
        Guid id,
        [FromQuery] string? armLeaseId,
        [FromQuery] string? runtimeId,
        [FromQuery] string? runtimeId,
        CancellationToken cancellationToken)
    {
        runtimeId = string.IsNullOrWhiteSpace(runtimeId) ? "real" : runtimeId.Trim().ToLowerInvariant();
        if (runtimeId is not ("real" or "sim") || runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = "runtimeId must identify a configured 'real' or 'sim' runtime." });

        runtimeId = string.IsNullOrWhiteSpace(runtimeId) ? "real" : runtimeId.Trim().ToLowerInvariant();
        if (runtimeId is not ("real" or "sim") || runtimes.Get(runtimeId) is null)
            return BadRequest(new { message = "runtimeId must identify a configured 'real' or 'sim' runtime." });

        var path = await maps.GetRobotMapPathAsync(id, cancellationToken);
        if (path is null) return NotFound();
        if (string.IsNullOrWhiteSpace(path))
            return Conflict(new { message = "Map has no robot-side PGM/YAML files or is not a saved occupancy map." });

        var activeZones = (await zones.GetAllAsync(cancellationToken))
            .Where(zone => zone.MapId == id && zone.IsActive)
            .Select(zone => new NavigationZonePolicy(
                zone.ZoneType, zone.MinX, zone.MinY, zone.MaxX, zone.MaxY, zone.MaxSpeed,
                zone.Width, zone.Height, zone.RotationAngle))
            .ToList();
        var expectedZoneCounts = activeZones
            .GroupBy(zone => zone.ZoneType.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key.ToUpperInvariant(),
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        var result = await runtimes.StartNavigationModeAsync(
            runtimeId, id, path, activeZones, armLeaseId, cancellationToken);
        if (!result.Success)
            return Conflict(new { message = result.Error });

        if (!RuntimeConfirmedZonePolicies(result.Payload, expectedZoneCounts))
        {
            await runtimes.StopNavigationModeAsync(runtimeId, cancellationToken);
            return Conflict(new
            {
                message = "The robot runtime did not confirm applying every active navigation zone policy. Nav2 was stopped to avoid navigating with incomplete zone configuration.",
                expectedZoneCounts,
            });
        }

        return Accepted(result.Payload);
    }

    /// <summary>Cập nhật thông tin và dữ liệu bản đồ.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, MapWriteRequest request, CancellationToken cancellationToken)
    {
        var map = await maps.UpdateAsync(id, request, cancellationToken);
        return map is null ? NotFound() : Ok(map);
    }

    /// <summary>Xóa bản đồ theo ID.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await maps.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    private static bool RuntimeConfirmedZonePolicies(
        System.Text.Json.JsonElement? payload,
        IReadOnlyDictionary<string, int> expectedZoneCounts)
    {
        if (payload is not { ValueKind: System.Text.Json.JsonValueKind.Object } response
            || !response.TryGetProperty("zone_count", out var totalElement)
            || !totalElement.TryGetInt32(out var reportedTotal)
            || reportedTotal != expectedZoneCounts.Values.Sum()
            || !response.TryGetProperty("zone_counts_by_type", out var typeCountsElement)
            || typeCountsElement.ValueKind != System.Text.Json.JsonValueKind.Object)
            return false;

        var reportedCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in typeCountsElement.EnumerateObject())
        {
            if (!property.Value.TryGetInt32(out var count) || count < 0
                || !reportedCounts.TryAdd(property.Name, count))
                return false;
        }

        return reportedCounts.Count == expectedZoneCounts.Count
            && expectedZoneCounts.All(expected =>
                reportedCounts.TryGetValue(expected.Key, out var actual)
                && actual == expected.Value);
    }
}
