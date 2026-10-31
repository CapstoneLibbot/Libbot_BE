using LiBot.Application.Models.Aisle;

namespace LiBot.Application.Interfaces;

public interface IAisleRepository
{
    Task<(IReadOnlyList<AisleResponse> Items, int TotalCount)> SearchAsync(AisleQuery query, CancellationToken cancellationToken);
    Task<AisleResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<bool> FloorExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<AisleResponse> CreateAsync(AisleWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<AisleResponse?> UpdateAsync(Guid id, AisleWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
