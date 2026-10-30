using LiBot.Application.Models.Book;

namespace LiBot.Application.Interfaces;

public interface IBookRepository
{
    Task<(IReadOnlyList<BookResponse> Items, int TotalCount)> SearchAsync(BookQuery query, CancellationToken cancellationToken);
    Task<BookResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> IsbnExistsAsync(string isbn, Guid? excludingBookId, CancellationToken cancellationToken);
    Task<BookResponse> CreateAsync(BookWriteRequest request, CancellationToken cancellationToken);
    Task<BookResponse?> UpdateAsync(Guid id, BookWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
