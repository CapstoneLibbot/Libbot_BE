using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Enums;
using LiBot.Domain.Library;

namespace LiBot.Infrastructure.Services;

public sealed class LibraryMapService(ILibraryMapRepository repository) : ILibraryMapService
{
    public async Task<IReadOnlyList<MapResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var maps = await repository.GetAllAsync(occupancyMaps: false, cancellationToken);
        return maps.OrderBy(map => map.Name).Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<EndpointDto>> GetEndpointsByMapIdAsync(Guid mapId, CancellationToken cancellationToken)
    {
        var endpoints = await repository.GetEndpointsByMapIdAsync(mapId, cancellationToken);
        return endpoints.Select(endpoint => ToResourceResponse<Endpoint, EndpointDto>(endpoint)).ToList();
    }

    public async Task<IReadOnlyList<ZoneDto>> GetZonesByMapIdAsync(Guid mapId, CancellationToken cancellationToken)
    {
        var zones = await repository.GetZonesByMapIdAsync(mapId, cancellationToken);
        return zones.Select(zone => ToResourceResponse<Zone, ZoneDto>(zone)).ToList();
    }

    public Task<IReadOnlyList<SavedRobotMapResponse>> GetRobotMapsAsync(CancellationToken cancellationToken) =>
        repository.GetRobotMapSummariesAsync(cancellationToken);

    public Task<SavedRobotMapResponse?> GetLatestRobotMapAsync(CancellationToken cancellationToken) =>
        repository.GetLatestRobotMapSummaryAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        repository.ExistsAsync(id, cancellationToken);

    public async Task<MapResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var map = await repository.GetByIdAsync(id, tracking: false, cancellationToken);
        return map is null ? null : ToResponse(map);
    }

    public async Task<MapResponse> CreateAsync(MapWriteRequest request, CancellationToken cancellationToken)
    {
        if (request.FloorId == Guid.Empty)
            throw new ArgumentException("floorId is required.", nameof(request));
        var status = ParseStatus(request.Status);
        var map = new LibraryMap(request.Name, request.Version ?? string.Empty, request.Width, request.Height,
            request.Data?.GetRawText() ?? "{}", request.FloorId, status);
        await repository.AddAsync(map, cancellationToken);
        return ToResponse(map);
    }

    public async Task<MapResponse> SaveRobotSnapshotAsync(string name, OccupancyGridDto grid, string robotPath,
        CancellationToken cancellationToken)
    {
        var mapName = name.Trim();
        var version = (await repository.CountByNameAsync(mapName, cancellationToken) + 1).ToString();
        var data = JsonSerializer.Serialize(new
        {
            format = "occupancy-grid-v1",
            resolution = grid.Resolution,
            origin = new[] { grid.OriginX, grid.OriginY, grid.OriginYaw },
            timestamp = grid.Timestamp,
            cells = grid.Data,
            robotPath
        });
        var floorId = await repository.GetFirstFloorIdAsync(cancellationToken)
            ?? throw new InvalidOperationException("Create a floor before saving a robot map.");
        var map = new LibraryMap(mapName, version, checked((int)grid.Width), checked((int)grid.Height), data, floorId);
        await repository.AddAsync(map, cancellationToken);
        return ToResponse(map);
    }

    public async Task<string?> GetRobotMapPathAsync(Guid id, CancellationToken cancellationToken)
    {
        var map = await repository.GetByIdAsync(id, tracking: false, cancellationToken);
        if (map is null) return null;
        try
        {
            using var json = JsonDocument.Parse(map.MapData);
            return json.RootElement.TryGetProperty("robotPath", out var path) && path.ValueKind == JsonValueKind.String
                ? path.GetString()
                : string.Empty;
        }
        catch (JsonException) { return string.Empty; }
    }

    public async Task<MapResponse?> UpdateAsync(Guid id, MapWriteRequest request, CancellationToken cancellationToken)
    {
        var map = await repository.GetByIdAsync(id, tracking: true, cancellationToken);
        if (map is null) return null;
        map.Update(request.Name, request.Version ?? map.Version, request.Width, request.Height,
            request.Data?.GetRawText() ?? map.MapData,
            request.FloorId == Guid.Empty ? map.FloorId : request.FloorId,
            request.Status is null ? map.Status : ParseStatus(request.Status));
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(map);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var map = await repository.GetByIdAsync(id, tracking: true, cancellationToken);
        if (map is null) return false;
        repository.Remove(map);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static MapResponse ToResponse(LibraryMap map) =>
        new(map.Id, map.Name, map.Width, map.Height, ParseData(map.MapData), map.Version, map.FloorId,
            map.Status.ToString());

    private static TDto ToResourceResponse<TEntity, TDto>(TEntity entity)
        where TEntity : class
        where TDto : class, ILibraryResourceDto<TEntity>, new()
    {
        var dto = new TDto();
        dto.LoadFrom(entity);
        return dto;
    }

    private static JsonElement? ParseData(string value)
    {
        try { return JsonDocument.Parse(value).RootElement.Clone(); }
        catch (JsonException) { return null; }
    }

    private static MapStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return MapStatus.Active;
        if (Enum.TryParse<MapStatus>(value, true, out var status) && Enum.IsDefined(status)) return status;
        throw new ArgumentException("status must be Active or Archived.", nameof(value));
    }
}
