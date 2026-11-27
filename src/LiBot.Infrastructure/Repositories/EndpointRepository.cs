using LiBot.Application.Interfaces;
using LiBot.Application.Models.Endpoint;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class EndpointRepository(LiBotDbContext db) : IEndpointRepository
{
    public async Task<(IReadOnlyList<EndpointResponse> Items, int TotalCount)> SearchAsync(
        EndpointQuery query,
        CancellationToken cancellationToken)
    {
        var source = db.Endpoints.AsNoTracking().AsQueryable();
        if (query.MapId is Guid mapId)
            source = source.Where(endpoint => endpoint.MapId == mapId);
        if (query.IsActive is bool isActive)
            source = source.Where(endpoint => endpoint.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(endpoint => EF.Functions.Like(endpoint.Name, $"%{search}%"));
        }

        var total = await source.CountAsync(cancellationToken);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            _ => descending
                ? source.OrderByDescending(endpoint => endpoint.Name)
                : source.OrderBy(endpoint => endpoint.Name)
        };
        var endpoints = await source.Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize).ToListAsync(cancellationToken);
        return (await ProjectAsync(endpoints, query.Expand, cancellationToken), total);
    }

    public async Task<EndpointResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var endpoint = await db.Endpoints.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return endpoint is null ? null : (await ProjectAsync([endpoint], expand, cancellationToken)).Single();
    }

    public Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.Maps.AnyAsync(map => map.Id == id, cancellationToken);

    public async Task<EndpointResponse> CreateAsync(EndpointWriteRequest request, CancellationToken cancellationToken)
    {
        var endpoint = new Endpoint(request.MapId, request.Name, request.PositionX, request.PositionY,
            request.Theta, request.Description, request.EndpointType);
        if (!request.IsActive) endpoint.Deactivate();
        db.Endpoints.Add(endpoint);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(endpoint.Id, null, cancellationToken))!;
    }

    public async Task<EndpointResponse?> UpdateAsync(Guid id, EndpointWriteRequest request, CancellationToken cancellationToken)
    {
        var endpoint = await db.Endpoints.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (endpoint is null) return null;
        endpoint.Update(request.MapId, request.Name, request.PositionX, request.PositionY, request.Theta,
            request.Description, request.EndpointType, request.IsActive);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, null, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var endpoint = await db.Endpoints.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (endpoint is null) return false;
        db.Endpoints.Remove(endpoint);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<EndpointResponse>> ProjectAsync(
        IReadOnlyList<Endpoint> endpoints,
        string? expand,
        CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mapIds = endpoints.Select(endpoint => endpoint.MapId).Distinct().ToArray();
        var maps = expansions.Contains("map")
            ? await db.Maps.AsNoTracking().Where(map => mapIds.Contains(map.Id))
                .ToDictionaryAsync(map => map.Id, cancellationToken)
            : [];

        return endpoints.Select(endpoint => new EndpointResponse(
            endpoint.Id,
            endpoint.MapId,
            endpoint.Name,
            endpoint.Description,
            endpoint.EndpointType,
            endpoint.PositionX,
            endpoint.PositionY,
            endpoint.Theta,
            endpoint.IsActive,
            maps.TryGetValue(endpoint.MapId, out var map)
                ? new EndpointMapResponse(map.Id, map.Name, map.Version, map.Width, map.Height, map.MapData)
                : null)).ToList();
    }
}
