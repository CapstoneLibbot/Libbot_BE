using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class BookGuidanceRepository(LiBotDbContext db) : IBookGuidanceRepository
{
    public async Task<BookSearchResponse> SearchAsync(
        string? query,
        string? category,
        bool exact,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var booksQuery =
            from book in db.Books.AsNoTracking()
            join bookCategory in db.BookCategories.AsNoTracking() on book.CategoryId equals bookCategory.Id into categories
            from bookCategory in categories.DefaultIfEmpty()
            select new { Book = book, CategoryName = bookCategory == null ? null : bookCategory.Name };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            booksQuery = exact
                ? booksQuery.Where(item => item.Book.Title == term || item.Book.Isbn == term || item.Book.Author == term)
                : booksQuery.Where(item =>
                    EF.Functions.Like(item.Book.Title, $"%{term}%") ||
                    EF.Functions.Like(item.Book.Isbn, $"%{term}%") ||
                    EF.Functions.Like(item.Book.Author, $"%{term}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryTerm = category.Trim().ToLowerInvariant();
            booksQuery = booksQuery.Where(item =>
                item.CategoryName != null &&
                EF.Functions.Like(item.CategoryName.ToLower(), $"%{categoryTerm}%"));
        }

        var totalCount = await booksQuery.CountAsync(cancellationToken);
        var books = await booksQuery
            .OrderBy(item => item.Book.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new BookSearchItem(
                item.Book.Id,
                item.Book.Isbn,
                item.Book.Title,
                item.Book.Author,
                item.CategoryName,
                db.BookItems.Count(bookItem => bookItem.BookId == item.Book.Id && bookItem.Status == LiBot.Domain.Enums.BookItemStatus.Available),
                item.Book.Image))
            .ToListAsync(cancellationToken);

        return new BookSearchResponse(books, page, pageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<BookGuidanceDetail?> GetBookAsync(Guid bookId, CancellationToken cancellationToken)
    {
        var book = await (
            from item in db.Books.AsNoTracking()
            join category in db.BookCategories.AsNoTracking() on item.CategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where item.Id == bookId
            select new
            {
                item.Id,
                item.Isbn,
                item.Title,
                item.Author,
                item.Publisher,
                item.PublicationYear,
                item.Image,
                Classification = category == null ? null : category.Name
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (book is null) return null;

        var copies = await db.BookItems
            .AsNoTracking()
            .Where(copy => copy.BookId == bookId)
            .OrderBy(copy => copy.Barcode)
            .Select(copy => new { copy.Id, copy.Barcode, copy.Status })
            .ToListAsync(cancellationToken);

        var locationsByCopyId = await (
            from copy in db.BookItems.AsNoTracking()
            join level in db.ShelfLevels.AsNoTracking() on copy.ExpectedShelfLevelId equals level.Id
            join shelf in db.Shelves.AsNoTracking() on level.ShelfId equals shelf.Id
            join aisle in db.Aisles.AsNoTracking() on shelf.AisleId equals aisle.Id
            join floor in db.Floors.AsNoTracking() on aisle.FloorId equals floor.Id
            where copy.BookId == bookId
            select new
            {
                copy.Id,
                Location = new BookLocation(
                    floor.Id, floor.Name, floor.FloorNumber, aisle.Id, aisle.Name, aisle.Name,
                    shelf.Id, shelf.Code, shelf.Name, level.Id, level.LevelNumber,
                    null)
            })
            .ToDictionaryAsync(item => item.Id, item => item.Location, cancellationToken);

        var bookItems = copies
            .Select(copy => new BookGuidanceBookItem(
                copy.Id, copy.Barcode, copy.Status.ToString(), locationsByCopyId.GetValueOrDefault(copy.Id)))
            .ToList();

        return new BookGuidanceDetail(
            book.Id, book.Isbn, book.Title, book.Author, book.Publisher, book.PublicationYear,
            book.Classification, ["VIEW_LOCATION", "START_GUIDANCE", "RATE"], book.Image)
        {
            BookItems = bookItems
        };
    }

    public async Task<FeedbackResponse?> SaveFeedbackAsync(
        FeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!await db.Jobs.AnyAsync(job => job.Id == request.TaskId, cancellationToken)) return null;

        var feedback = new Feedback(request.TaskId, request.Rating, request.Comment);
        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return ToFeedbackResponse(feedback);
    }

    public async Task<IReadOnlyList<FeedbackResponse>> GetFeedbackAsync(Guid? taskId, CancellationToken cancellationToken)
    {
        var query = db.Feedback.AsNoTracking();
        if (taskId.HasValue) query = query.Where(item => item.JobId == taskId.Value);
        return await query.OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new FeedbackResponse(item.Id, item.JobId, item.Rating, item.Comment))
            .ToListAsync(cancellationToken);
    }

    public async Task<FeedbackResponse?> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var feedback = await db.Feedback.AsNoTracking().FirstOrDefaultAsync(item => item.Id == feedbackId, cancellationToken);
        return feedback is null ? null : ToFeedbackResponse(feedback);
    }

    public async Task<FeedbackResponse?> UpdateFeedbackAsync(Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken)
    {
        var feedback = await db.Feedback.FirstOrDefaultAsync(item => item.Id == feedbackId, cancellationToken);
        if (feedback is null) return null;
        feedback.Update(request.Rating, request.Comment);
        await db.SaveChangesAsync(cancellationToken);
        return ToFeedbackResponse(feedback);
    }

    public async Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var feedback = await db.Feedback.FirstOrDefaultAsync(item => item.Id == feedbackId, cancellationToken);
        if (feedback is null) return false;
        db.Feedback.Remove(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static FeedbackResponse ToFeedbackResponse(Feedback feedback) =>
        new(feedback.Id, feedback.JobId, feedback.Rating, feedback.Comment);
}
