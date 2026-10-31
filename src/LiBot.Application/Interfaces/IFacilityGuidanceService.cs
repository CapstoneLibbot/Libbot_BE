using System.Text.Json;
using LiBot.Application.Models;

namespace LiBot.Application.Workflow.FacilityGuidance;

public interface IFacilityGuidanceService
{
    Task<bool> IsSimulationTaskAsync(Guid taskId, CancellationToken cancellationToken);

    Task<FacilitySearchResponse> SearchAsync(
        string? query,
        string matchMode,
        CancellationToken cancellationToken);

    Task<FacilityGuidanceDetail?> GetFacilityAsync(
        Guid facilityId,
        CancellationToken cancellationToken);

    Task<FacilityFeedbackResponse?> SaveFeedbackAsync(
        FacilityFeedbackRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<FacilityFeedbackResponse>> GetFeedbackAsync(Guid? taskId, CancellationToken cancellationToken);
    Task<FacilityFeedbackResponse?> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken cancellationToken);
    Task<FacilityFeedbackResponse?> UpdateFeedbackAsync(Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken);

    Task<FacilityGuidanceLoadedWorkflow?> LoadWorkflowAsync(CancellationToken cancellationToken);

    Task<FacilityGuidanceStartResult> StartAsync(
        StartFacilityGuidanceRequest request,
        string backendBaseUrl,
        CancellationToken cancellationToken);

    Task<JsonElement> SubmitEventAsync(
        FacilityGuidanceEventRequest request,
        CancellationToken cancellationToken);

    Task<JsonElement> GetExecutionAsync(
        string executionId,
        CancellationToken cancellationToken);

    Task<bool> CompleteTaskAsync(
        Guid taskId,
        CompleteFacilityGuidanceRequest request,
        CancellationToken cancellationToken);
}
