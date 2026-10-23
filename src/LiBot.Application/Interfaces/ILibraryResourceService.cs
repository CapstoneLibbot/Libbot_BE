using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface ILibraryResourceService<TEntity, TDto>
    where TEntity : class
    where TDto : class, ILibraryResourceDto<TEntity>, new()
{
    Task<IReadOnlyList<TDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TDto> CreateAsync(TDto dto, CancellationToken cancellationToken);
    Task<TDto?> UpdateAsync(Guid id, TDto dto, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
