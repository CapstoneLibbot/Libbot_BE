using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Domain.Mission;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Repositories;

public sealed class FacilityGuidanceRepository(LiBotDbContext db) : IFacilityGuidanceRepository
{
    private const string ReadingRoomName = "Phòng đọc sách";


    public async Task<FacilitySearchResponse> SearchAsync(
        string? query,
        bool exact,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var term = query?.Trim() ?? string.Empty;
        var facilitiesQuery =
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            join floor in db.Floors.AsNoTracking() on map.FloorId equals floor.Id
            where endpoint.IsActive && endpoint.EndpointType == "WAYPOINT"
            select new { Endpoint = endpoint, Map = map, Floor = floor };

        if (!string.IsNullOrEmpty(term))
        {
            facilitiesQuery = exact
                ? facilitiesQuery.Where(item => item.Endpoint.Name == term
                    || item.Endpoint.Description == term
                    || item.Floor.Name == term)
                : facilitiesQuery.Where(item =>
                    EF.Functions.Like(item.Endpoint.Name, $"%{term}%") ||
                    (item.Endpoint.Description != null && EF.Functions.Like(item.Endpoint.Description, $"%{term}%")) ||
                    EF.Functions.Like(item.Floor.Name, $"%{term}%"));
        }

        var totalCount = await facilitiesQuery.CountAsync(cancellationToken);
        var facilities = await facilitiesQuery
            .OrderBy(item => item.Endpoint.Name)
            .ThenBy(item => item.Endpoint.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new FacilitySearchItem(
                item.Endpoint.Id,
                item.Endpoint.Name,
                item.Endpoint.Name == ReadingRoomName ? "READING_ROOM" : "ENDPOINT",
                item.Map.Id,
                item.Map.Name))
            .ToListAsync(cancellationToken);

        return new FacilitySearchResponse(
            facilities,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public async Task<IReadOnlyList<FacilityGuidanceDetail>> GetAllFacilitiesAsync(CancellationToken cancellationToken) =>
        await (
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            join floor in db.Floors.AsNoTracking() on map.FloorId equals floor.Id
            where endpoint.IsActive && endpoint.EndpointType == "WAYPOINT"
            orderby endpoint.Name
            select ToFacilityDetail(floor, endpoint, map))
            .ToListAsync(cancellationToken);

    public Task<FacilityGuidanceDetail?> GetReadingRoomAsync(CancellationToken cancellationToken) =>
        (
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            join floor in db.Floors.AsNoTracking() on map.FloorId equals floor.Id
            where endpoint.IsActive && endpoint.EndpointType == "WAYPOINT" && endpoint.Name == ReadingRoomName
            orderby endpoint.Id
            select ToFacilityDetail(floor, endpoint, map))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<FacilityGuidanceDetail?> GetFacilityAsync(Guid facilityId, CancellationToken cancellationToken) =>
        (
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            join floor in db.Floors.AsNoTracking() on map.FloorId equals floor.Id
            where endpoint.Id == facilityId && endpoint.IsActive && endpoint.EndpointType == "WAYPOINT"
            select ToFacilityDetail(floor, endpoint, map))
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
        return await query.OrderByDescending(item => item.Id)
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

    private static FacilityGuidanceDetail ToFacilityDetail(
        LiBot.Domain.Library.Floor floor,
        LiBot.Domain.Library.Endpoint endpoint,
        LiBot.Domain.Library.LibraryMap map) =>
        new(
            endpoint.Id,
            endpoint.Name,
            endpoint.Name == ReadingRoomName ? "READING_ROOM" : "ENDPOINT",
            endpoint.Description ?? floor.Name,
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
            new[] { "VIEW_LOCATION", "START_GUIDANCE", "RATE" });
}
