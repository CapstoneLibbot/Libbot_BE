using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface ILibraryMapService
{
    Task<IReadOnlyList<MapResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<EndpointDto>> GetEndpointsByMapIdAsync(Guid mapId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ZoneDto>> GetZonesByMapIdAsync(Guid mapId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SavedRobotMapResponse>> GetRobotMapsAsync(CancellationToken cancellationToken);
    Task<SavedRobotMapResponse?> GetLatestRobotMapAsync(CancellationToken cancellationToken);
    Task<MapResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MapResponse> CreateAsync(MapWriteRequest request, CancellationToken cancellationToken);
    Task<MapResponse> SaveRobotSnapshotAsync(string name, OccupancyGridDto grid, string robotPath, CancellationToken cancellationToken);
    Task<string?> GetRobotMapPathAsync(Guid id, CancellationToken cancellationToken);
    Task<MapResponse?> UpdateAsync(Guid id, MapWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
