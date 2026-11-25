using LiBot.Application.Interfaces;
using LiBot.Application.Models;

namespace LiBot.Application.Services;

public sealed class BookItemService(IBookItemRepository repository) : IBookItemService
{
    public async Task<IReadOnlyList<BookItemDto>> GetByBookIdAsync(
        Guid bookId,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetByBookIdAsync(bookId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<BookItemDto>> GetByShelfLevelIdAsync(
        Guid shelfLevelId,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetByShelfLevelIdAsync(shelfLevelId, cancellationToken);
        return items.Select(ToDto).ToList();
    }

    private static BookItemDto ToDto(LiBot.Domain.Library.BookItem entity)
    {
        var dto = new BookItemDto();
        dto.LoadFrom(entity);
        return dto;
    }
}
