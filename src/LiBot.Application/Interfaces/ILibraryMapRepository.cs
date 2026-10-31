using LiBot.Domain.Library;

namespace LiBot.Application.Interfaces;

public interface ILibraryMapRepository
{
    Task<IReadOnlyList<LibraryMap>> GetAllAsync(bool occupancyMaps, CancellationToken cancellationToken);
    Task<IReadOnlyList<Endpoint>> GetEndpointsByMapIdAsync(Guid mapId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Zone>> GetZonesByMapIdAsync(Guid mapId, CancellationToken cancellationToken);
    Task<LibraryMap?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<int> CountByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(LibraryMap map, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void Remove(LibraryMap map);
}
