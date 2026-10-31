using LiBot.Application.Interfaces;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class LibraryMapRepository(LiBotDbContext db) : ILibraryMapRepository
{
    public async Task<IReadOnlyList<LibraryMap>> GetAllAsync(bool occupancyMaps, CancellationToken cancellationToken) =>
        await db.Maps.AsNoTracking()
            .Where(map => occupancyMaps
                ? EF.Functions.Like(map.MapData, "%occupancy-grid-v1%")
                : !EF.Functions.Like(map.MapData, "%occupancy-grid-v1%"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Endpoint>> GetEndpointsByMapIdAsync(Guid mapId, CancellationToken cancellationToken) =>
        await db.Endpoints.AsNoTracking()
            .Where(endpoint => endpoint.MapId == mapId)
            .OrderBy(endpoint => endpoint.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Zone>> GetZonesByMapIdAsync(Guid mapId, CancellationToken cancellationToken) =>
        await db.Zones.AsNoTracking()
            .Where(zone => zone.MapId == mapId)
            .OrderBy(zone => zone.Name)
            .ToListAsync(cancellationToken);

    public Task<LibraryMap?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken)
    {
        var maps = tracking ? db.Maps : db.Maps.AsNoTracking();
        return maps.FirstOrDefaultAsync(map => map.Id == id, cancellationToken);
    }

    public Task<int> CountByNameAsync(string name, CancellationToken cancellationToken) =>
        db.Maps.CountAsync(map => map.Name == name, cancellationToken);

    public async Task AddAsync(LibraryMap map, CancellationToken cancellationToken)
    {
        await db.Maps.AddAsync(map, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public void Remove(LibraryMap map) => db.Maps.Remove(map);
}
