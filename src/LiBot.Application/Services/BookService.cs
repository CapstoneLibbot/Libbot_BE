using LiBot.Application.Interfaces;
using LiBot.Application.Models.Book;

namespace LiBot.Application.Services;

public class BookService(IBookRepository repository) : IBookService
{
    public async Task<BookListResponse> GetAllAsync(BookQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var normalized = new BookQuery(
            page, pageSize, query.Search, query.CategoryId, query.SortBy, query.SortOrder, query.Expand);
        var (items, totalCount) = await repository.SearchAsync(normalized, cancellationToken);
        return new BookListResponse(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize),
            normalized.SortBy.ToLowerInvariant(),
            normalized.SortOrder.ToLowerInvariant());
    }

    public Task<BookResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(id, expand, cancellationToken);

    public async Task<BookResponse> CreateAsync(BookWriteRequest request, CancellationToken cancellationToken)
    {
        ValidateRequiredFields(request);
        await ValidateReferencesAsync(request.CategoryId, cancellationToken);
        if (await repository.IsbnExistsAsync(request.Isbn.Trim(), null, cancellationToken))
            throw new InvalidOperationException("A book with this ISBN already exists.");
        return await repository.CreateAsync(request, cancellationToken);
    }

    public async Task<BookResponse?> UpdateAsync(Guid id, BookWriteRequest request, CancellationToken cancellationToken)
    {
        ValidateRequiredFields(request);
        await ValidateReferencesAsync(request.CategoryId, cancellationToken);
        if (await repository.IsbnExistsAsync(request.Isbn.Trim(), id, cancellationToken))
            throw new InvalidOperationException("A book with this ISBN already exists.");
        return await repository.UpdateAsync(id, request, cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        repository.DeleteAsync(id, cancellationToken);

    private static void ValidateRequiredFields(BookWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Isbn)
            || string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.Author))
            throw new ArgumentException("isbn, title and author are required.");
        if (request.PublicationYear is < 0)
            throw new ArgumentException("publicationYear must not be negative.");
    }

    private async Task ValidateReferencesAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId is Guid id && !await repository.CategoryExistsAsync(id, cancellationToken))
            throw new KeyNotFoundException("Book category was not found.");
    }
}
