using LiBot.Application.Models.Shelf;
using LiBot.Application.Models.ShelfLevel;

namespace LiBot.Application.Interfaces;

public interface IShelfService
{
    Task<ShelfListResponse> GetAllAsync(ShelfQuery query, CancellationToken cancellationToken);
    Task<ShelfResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<ShelfResponse> CreateAsync(ShelfWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<ShelfResponse?> UpdateAsync(Guid id, ShelfWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<ShelfLevelListResponse> GetLevelsAsync(ShelfLevelQuery query, CancellationToken cancellationToken);
    Task<ShelfLevelResponse?> GetLevelByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ShelfLevelResponse> CreateLevelAsync(ShelfLevelWriteRequest request, CancellationToken cancellationToken);
    Task<ShelfLevelResponse?> UpdateLevelAsync(Guid id, ShelfLevelWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteLevelAsync(Guid id, CancellationToken cancellationToken);
}
