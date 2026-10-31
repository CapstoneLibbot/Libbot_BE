using LiBot.Application.Models.Zone;

namespace LiBot.Application.Interfaces;

public interface IZoneRepository
{
    Task<(IReadOnlyList<ZoneResponse> Items, int TotalCount)> SearchAsync(ZoneQuery query, CancellationToken cancellationToken);
    Task<ZoneResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<bool> MapExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<ZoneResponse> CreateAsync(ZoneWriteRequest request, CancellationToken cancellationToken);
    Task<ZoneResponse?> UpdateAsync(Guid id, ZoneWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
