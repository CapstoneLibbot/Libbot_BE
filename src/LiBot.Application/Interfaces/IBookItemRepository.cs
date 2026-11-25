using LiBot.Domain.Library;

namespace LiBot.Application.Interfaces;

public interface IBookItemRepository
{
    Task<IReadOnlyList<BookItem>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookItem>> GetByShelfLevelIdAsync(Guid shelfLevelId, CancellationToken cancellationToken);
}
