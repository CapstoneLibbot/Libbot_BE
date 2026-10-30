using LiBot.Application.Models.Area;

namespace LiBot.Application.Interfaces;

public interface IAreaService
{
    Task<AreaListResponse> GetAllAsync(AreaQuery query, CancellationToken cancellationToken);
    Task<AreaResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<AreaResponse> CreateAsync(AreaWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<AreaResponse?> UpdateAsync(Guid id, AreaWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
