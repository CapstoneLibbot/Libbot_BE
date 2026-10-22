using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IBookGuidanceRepository
{
    Task<BookSearchResponse> SearchAsync(
        string? query,
        string? category,
        bool exact,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BookGuidanceDetail?> GetBookAsync(Guid bookId, CancellationToken cancellationToken);
    Task<FeedbackResponse?> SaveFeedbackAsync(FeedbackRequest request, CancellationToken cancellationToken);
}
