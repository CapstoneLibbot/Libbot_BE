using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IFacilityGuidanceRepository
{
    Task<FacilitySearchResponse> SearchAsync(
        string query,
        bool exact,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FacilityGuidanceDetail>> GetAllFacilitiesAsync(CancellationToken cancellationToken);
    Task<FacilityGuidanceDetail?> GetReadingRoomAsync(CancellationToken cancellationToken);
    Task<FacilityGuidanceDetail?> GetFacilityAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FacilityFeedbackResponse?> SaveFeedbackAsync(FacilityFeedbackRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<FacilityFeedbackResponse>> GetFeedbackAsync(Guid? taskId, CancellationToken cancellationToken);
    Task<FacilityFeedbackResponse?> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken cancellationToken);
    Task<FacilityFeedbackResponse?> UpdateFeedbackAsync(Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteFeedbackAsync(Guid feedbackId, CancellationToken cancellationToken);
}
