using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IBookItemService
{
    Task<IReadOnlyList<BookItemDto>> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken);
    Task<IReadOnlyList<BookItemDto>> GetByShelfLevelIdAsync(Guid shelfLevelId, CancellationToken cancellationToken);
}
