using System.Reflection;
using LiBot.Application.Interfaces;

namespace LiBot.Application.Services;

public sealed class LibraryResourceService<TEntity>(IGenericRepository<TEntity> repository)
    : ILibraryResourceService<TEntity> where TEntity : class
{
    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, cancellationToken);

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken)
    {
        repository.Add(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<TEntity?> UpdateAsync(Guid id, TEntity replacement, CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null) return null;
        foreach (var property in typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.Name is "Id" or "CreatedAtUtc" or "UpdatedAtUtc" || property.GetSetMethod(true) is null) continue;
            property.SetValue(current, property.GetValue(replacement));
        }
        await repository.SaveChangesAsync(cancellationToken);
        return current;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.GetByIdAsync(id, cancellationToken) is not null;

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        if (entity is null) return false;
        repository.Remove(entity);
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
