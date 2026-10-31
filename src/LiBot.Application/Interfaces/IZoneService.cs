using LiBot.Application.Models.Zone;

namespace LiBot.Application.Interfaces;

public interface IZoneService
{
    Task<ZoneListResponse> GetAllAsync(ZoneQuery query, CancellationToken cancellationToken);
    Task<ZoneResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<ZoneResponse> CreateAsync(ZoneWriteRequest request, CancellationToken cancellationToken);
    Task<ZoneResponse?> UpdateAsync(Guid id, ZoneWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
