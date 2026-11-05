using System.Text.Json;
using LiBot.Application.Models;
using LiBot.Application.Models.Endpoint;

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
    Task<IReadOnlyList<FeedbackResponse>> GetFeedbackAsync(Guid? taskId, CancellationToken cancellationToken);
    Task<FeedbackResponse?> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken cancellationToken);
    Task<FeedbackResponse?> UpdateFeedbackAsync(Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken);

    Task<BookGuidanceLoadedWorkflow?> LoadWorkflowAsync(CancellationToken cancellationToken);

    Task<BookGuidanceStartResult> StartAsync(
        StartBookGuidanceRequest request,
        EndpointResponse endpoint,
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
