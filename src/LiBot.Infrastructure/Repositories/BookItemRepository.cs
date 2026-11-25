using LiBot.Application.Interfaces;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class BookItemRepository(LiBotDbContext db) : IBookItemRepository
{
    public async Task<IReadOnlyList<BookItem>> GetByBookIdAsync(
        Guid bookId,
        CancellationToken cancellationToken) =>
        await db.BookItems
            .AsNoTracking()
            .Where(item => item.BookId == bookId)
            .OrderBy(item => item.Barcode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BookItem>> GetByShelfLevelIdAsync(
        Guid shelfLevelId,
        CancellationToken cancellationToken) =>
        await db.BookItems
            .AsNoTracking()
            .Where(item => item.CurrentShelfLevelId == shelfLevelId)
            .OrderBy(item => item.Barcode)
            .ToListAsync(cancellationToken);
}
