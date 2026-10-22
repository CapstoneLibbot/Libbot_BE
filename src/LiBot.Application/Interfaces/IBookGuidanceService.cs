using System.Text.Json;
using LiBot.Application.Models;

namespace LiBot.Application.Workflow.BookGuidance;

public interface IBookGuidanceService
{
    Task<bool> IsSimulationTaskAsync(Guid taskId, CancellationToken cancellationToken);

    Task<BookSearchResponse> SearchAsync(
        string? query,
        string? category,
        string matchMode,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<BookGuidanceDetail?> GetBookAsync(
        Guid bookId,
        CancellationToken cancellationToken);

    Task<FeedbackResponse?> SaveFeedbackAsync(
        FeedbackRequest request,
        CancellationToken cancellationToken);

    Task<BookGuidanceLoadedWorkflow?> LoadWorkflowAsync(CancellationToken cancellationToken);

    Task<BookGuidanceStartResult> StartAsync(
        StartBookGuidanceRequest request,
        string backendBaseUrl,
        CancellationToken cancellationToken);

    Task<JsonElement> SubmitEventAsync(
        BookGuidanceEventRequest request,
        CancellationToken cancellationToken);

    Task<JsonElement> GetExecutionAsync(
        string executionId,
        CancellationToken cancellationToken);

    Task<bool> CompleteTaskAsync(
        Guid taskId,
        CompleteBookGuidanceRequest request,
        CancellationToken cancellationToken);
}
