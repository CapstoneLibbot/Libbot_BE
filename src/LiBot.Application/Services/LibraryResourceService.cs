using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Common;

namespace LiBot.Application.Services;

public sealed class LibraryResourceService<TEntity, TDto>(ILibraryResourceRepository<TEntity> repository)
    : ILibraryResourceService<TEntity, TDto>
    where TEntity : class
    where TDto : class, ILibraryResourceDto<TEntity>, new()
{
    public async Task<IReadOnlyList<TDto>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(ToDto).ToList();

    public async Task<TDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<TDto> CreateAsync(TDto dto, CancellationToken cancellationToken)
    {
        var entity = dto.ToEntity();
        dto.ApplyTo(entity);
        repository.Add(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<TDto?> UpdateAsync(Guid id, TDto dto, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null) return null;
        dto.ApplyTo(current);
        if (current is Entity entity) entity.SetUpdated();
        await repository.SaveChangesAsync(cancellationToken);
        return ToDto(current);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TDto ToDto(TEntity entity)
    {
        var dto = new TDto();
        dto.LoadFrom(entity);
        return dto;
    }
}
