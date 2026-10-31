using LiBot.Application.Interfaces;
using LiBot.Application.Models.Book;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public class BookRepository(LiBotDbContext db) : IBookRepository
{
    public async Task<(IReadOnlyList<BookResponse> Items, int TotalCount)> SearchAsync(
        BookQuery query,
        CancellationToken cancellationToken)
    {
        var source = db.Books.AsNoTracking().AsQueryable();
        if (query.CategoryId is Guid categoryId)
            source = source.Where(book => book.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(book => EF.Functions.Like(book.Isbn, $"%{search}%")
                || EF.Functions.Like(book.Title, $"%{search}%")
                || EF.Functions.Like(book.Author, $"%{search}%")
                || (book.Publisher != null && EF.Functions.Like(book.Publisher, $"%{search}%")));
        }

        var total = await source.CountAsync(cancellationToken);
        var descending = string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        source = query.SortBy.ToLowerInvariant() switch
        {
            "isbn" => descending ? source.OrderByDescending(book => book.Isbn) : source.OrderBy(book => book.Isbn),
            "author" => descending ? source.OrderByDescending(book => book.Author) : source.OrderBy(book => book.Author),
            "publicationyear" => descending ? source.OrderByDescending(book => book.PublicationYear) : source.OrderBy(book => book.PublicationYear),
            "created" or "createdatutc" => descending ? source.OrderByDescending(book => book.CreatedAtUtc) : source.OrderBy(book => book.CreatedAtUtc),
            _ => descending ? source.OrderByDescending(book => book.Title) : source.OrderBy(book => book.Title)
        };

        var entities = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        return (await ProjectAsync(entities, query.Expand, cancellationToken), total);
    }

    public async Task<BookResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken)
    {
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return book is null ? null : (await ProjectAsync([book], expand, cancellationToken)).Single();
    }

    public Task<bool> CategoryExistsAsync(Guid id, CancellationToken cancellationToken) =>
        db.BookCategories.AnyAsync(category => category.Id == id, cancellationToken);

    public Task<bool> IsbnExistsAsync(string isbn, Guid? excludingBookId, CancellationToken cancellationToken) =>
        db.Books.AnyAsync(book => book.Isbn == isbn && book.Id != excludingBookId, cancellationToken);

    public async Task<BookResponse> CreateAsync(BookWriteRequest request, CancellationToken cancellationToken)
    {
        var book = new Book(request.Isbn, request.Title, request.Author, request.CategoryId,
            request.Publisher, request.PublicationYear, request.Image);
        db.Books.Add(book);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(book.Id, null, cancellationToken))!;
    }

    public async Task<BookResponse?> UpdateAsync(Guid id, BookWriteRequest request, CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null) return null;
        book.Update(request.Isbn, request.Title, request.Author, request.CategoryId,
            request.Publisher, request.PublicationYear, request.Image);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, null, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var book = await db.Books.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null) return false;
        db.Books.Remove(book);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<IReadOnlyList<BookResponse>> ProjectAsync(
        IReadOnlyList<Book> books,
        string? expand,
        CancellationToken cancellationToken)
    {
        var expansions = (expand ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var categoryIds = books.Where(book => book.CategoryId.HasValue)
            .Select(book => book.CategoryId!.Value).Distinct().ToArray();
        var bookIds = books.Select(book => book.Id).ToArray();
        var categories = expansions.Contains("category")
            ? await db.BookCategories.AsNoTracking().Where(category => categoryIds.Contains(category.Id))
                .ToDictionaryAsync(category => category.Id, cancellationToken)
            : [];
        var items = expansions.Contains("items")
            ? await db.BookItems.AsNoTracking().Where(item => bookIds.Contains(item.BookId)).ToListAsync(cancellationToken)
            : [];

        return books.Select(book => new BookResponse(
            book.Id,
            book.CategoryId,
            book.Isbn,
            book.Title,
            book.Author,
            book.Publisher,
            book.PublicationYear,
            book.Image,
            book.CreatedAtUtc,
            book.UpdatedAtUtc,
            book.CategoryId is Guid id && categories.TryGetValue(id, out var category)
                ? new BookCategoryResponse(category.Id, category.Name, category.Description)
                : null,
            expansions.Contains("items")
                ? items.Where(item => item.BookId == book.Id)
                    .Select(item => new BookItemResponse(item.Id, item.Barcode, item.ExpectedShelfLevelId,
                        item.CurrentShelfLevelId, item.LastInventoryAtUtc, item.Status,
                        item.CreatedAtUtc, item.UpdatedAtUtc)).ToList()
                : null)).ToList();
    }
}
