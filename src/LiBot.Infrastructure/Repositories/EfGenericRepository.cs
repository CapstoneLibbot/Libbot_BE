using LiBot.Application.Interfaces;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class EfGenericRepository<TEntity>(LiBotDbContext db) : IGenericRepository<TEntity>
    where TEntity : class
{
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Set<TEntity>().AsNoTracking().ToListAsync(cancellationToken);

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Set<TEntity>().FindAsync([id], cancellationToken);

    public void Add(TEntity entity) => db.Set<TEntity>().Add(entity);

    public void Remove(TEntity entity) => db.Set<TEntity>().Remove(entity);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
