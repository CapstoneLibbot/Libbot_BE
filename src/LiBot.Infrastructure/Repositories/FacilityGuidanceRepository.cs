using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class FacilityGuidanceRepository(LiBotDbContext db) : IFacilityGuidanceRepository
{
    public async Task<FacilitySearchResponse> SearchAsync(
        string query,
        bool exact,
        CancellationToken cancellationToken)
    {
        var term = query.Trim();
        var facilitiesQuery =
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            where endpoint.IsActive
            select new { Endpoint = endpoint, Map = map };

        facilitiesQuery = exact
            ? facilitiesQuery.Where(item => item.Endpoint.Name == term || item.Map.Name == term)
            : facilitiesQuery.Where(item =>
                EF.Functions.Like(item.Endpoint.Name, $"%{term}%") ||
                EF.Functions.Like(item.Map.Name, $"%{term}%"));

        var facilities = await facilitiesQuery
            .OrderBy(item => item.Endpoint.Name)
            .Select(item => new FacilitySearchItem(
                item.Endpoint.Id, item.Endpoint.Name, "ENDPOINT", item.Map.Id, item.Map.Name))
            .Take(50)
            .ToListAsync(cancellationToken);

        return new FacilitySearchResponse(facilities);
    }

    public Task<FacilityGuidanceDetail?> GetFacilityAsync(Guid facilityId, CancellationToken cancellationToken) =>
        (
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            where endpoint.Id == facilityId && endpoint.IsActive
            select new FacilityGuidanceDetail(
                endpoint.Id,
                endpoint.Name,
                "ENDPOINT",
                map.Name,
                new FacilityLocation(
                    map.Id,
                    map.Name,
                    map.Width,
                    map.Height,
                    new FacilityGuidanceEndpoint(
                        endpoint.Id,
                        endpoint.Name,
                        endpoint.PositionX,
                        endpoint.PositionY,
                        endpoint.Theta)),
                new[] { "VIEW_LOCATION", "RATE" }))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<FacilityFeedbackResponse?> SaveFeedbackAsync(
        FacilityFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!await db.Jobs.AnyAsync(job => job.Id == request.TaskId, cancellationToken)) return null;

        var feedback = new Feedback(request.TaskId, request.Rating, request.Comment);
        db.Feedback.Add(feedback);
        await db.SaveChangesAsync(cancellationToken);
        return ToFeedbackResponse(feedback);
    }

    public async Task<IReadOnlyList<FacilityFeedbackResponse>> GetFeedbackAsync(Guid? taskId, CancellationToken cancellationToken)
    {
        var query = db.Feedback.AsNoTracking();
        if (taskId.HasValue) query = query.Where(item => item.JobId == taskId.Value);
        return await query.OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new FacilityFeedbackResponse(item.Id, item.JobId, item.Rating, item.Comment))
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilityFeedbackResponse?> GetFeedbackByIdAsync(Guid feedbackId, CancellationToken cancellationToken)
    {
        var feedback = await db.Feedback.AsNoTracking().FirstOrDefaultAsync(item => item.Id == feedbackId, cancellationToken);
        return feedback is null ? null : ToFeedbackResponse(feedback);
    }

    public async Task<FacilityFeedbackResponse?> UpdateFeedbackAsync(Guid feedbackId, FeedbackUpdateRequest request, CancellationToken cancellationToken)
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

    private static FacilityFeedbackResponse ToFeedbackResponse(Feedback feedback) =>
        new(feedback.Id, feedback.JobId, feedback.Rating, feedback.Comment);
}
