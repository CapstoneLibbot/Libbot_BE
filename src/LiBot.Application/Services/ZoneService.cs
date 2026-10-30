using LiBot.Application.Interfaces;
using LiBot.Application.Models.Zone;

namespace LiBot.Application.Services;

public class ZoneService(IZoneRepository repository) : IZoneService
{
    public async Task<ZoneListResponse> GetAllAsync(ZoneQuery query, CancellationToken cancellationToken)
    {
        var normalized = new ZoneQuery(Math.Max(1, query.Page), Math.Clamp(query.PageSize, 1, 100), query.Search,
            query.MapId, query.ZoneType, query.IsActive, query.SortBy, query.SortOrder, query.Expand);
        var (items, total) = await repository.SearchAsync(normalized, cancellationToken);
        return new ZoneListResponse(items, normalized.Page, normalized.PageSize, total,
            (int)Math.Ceiling(total / (double)normalized.PageSize), normalized.SortBy.ToLowerInvariant(), normalized.SortOrder.ToLowerInvariant());
    }

    public Task<ZoneResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public async Task<ZoneResponse> CreateAsync(ZoneWriteRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        await ValidateMapAsync(request.MapId, cancellationToken);
        return await repository.CreateAsync(request, cancellationToken);
    }

    public async Task<ZoneResponse?> UpdateAsync(Guid id, ZoneWriteRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        await ValidateMapAsync(request.MapId, cancellationToken);
        return await repository.UpdateAsync(id, request, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => repository.DeleteAsync(id, cancellationToken);

    private static void Validate(ZoneWriteRequest request)
    {
        if (request.MapId == Guid.Empty || string.IsNullOrWhiteSpace(request.ZoneType)) throw new ArgumentException("mapId and zoneType are required.");
        if (!double.IsFinite(request.MaxSpeed) || !double.IsFinite(request.MinX) || !double.IsFinite(request.MinY)
            || !double.IsFinite(request.MaxX) || !double.IsFinite(request.MaxY)
            || (request.CorridorWidth is double width && !double.IsFinite(width)))
            throw new ArgumentException("zone numeric fields must be finite numbers.");
    }

    private async Task ValidateMapAsync(Guid mapId, CancellationToken cancellationToken)
    {
        if (!await repository.MapExistsAsync(mapId, cancellationToken)) throw new KeyNotFoundException("Map was not found.");
    }
}
