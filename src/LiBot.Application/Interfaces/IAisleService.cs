using LiBot.Application.Models.Aisle;

namespace LiBot.Application.Interfaces;

public interface IAisleService
{
    Task<AisleListResponse> GetAllAsync(AisleQuery query, CancellationToken cancellationToken);
    Task<AisleResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<AisleResponse> CreateAsync(AisleWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<AisleResponse?> UpdateAsync(Guid id, AisleWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
