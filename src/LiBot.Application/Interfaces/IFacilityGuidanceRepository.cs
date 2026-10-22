using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IFacilityGuidanceRepository
{
    Task<FacilitySearchResponse> SearchAsync(
        string query,
        bool exact,
        CancellationToken cancellationToken);

    Task<FacilityGuidanceDetail?> GetFacilityAsync(Guid facilityId, CancellationToken cancellationToken);
    Task<FacilityFeedbackResponse?> SaveFeedbackAsync(FacilityFeedbackRequest request, CancellationToken cancellationToken);
}
