using LiBot.Application.Models.Book;

namespace LiBot.Application.Interfaces;

public interface IBookService
{
    Task<BookListResponse> GetAllAsync(BookQuery query, CancellationToken cancellationToken);
    Task<BookResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<BookResponse> CreateAsync(BookWriteRequest request, CancellationToken cancellationToken);
    Task<BookResponse?> UpdateAsync(Guid id, BookWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
