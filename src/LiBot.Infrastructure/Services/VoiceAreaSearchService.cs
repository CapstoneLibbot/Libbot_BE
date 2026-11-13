using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Services;

public sealed class VoiceAreaSearchService(LiBotDbContext db) : IVoiceAreaSearchService
{
    public async Task<VoiceAreaSearchResponse> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var term = query.Trim().ToLowerInvariant();
        var areas =
            from endpoint in db.Endpoints.AsNoTracking()
            join map in db.Maps.AsNoTracking() on endpoint.MapId equals map.Id
            join floor in db.Floors.AsNoTracking() on map.FloorId equals floor.Id
            where endpoint.IsActive && endpoint.EndpointType == "WAYPOINT"
                  && endpoint.Name.ToLower().Contains(term)
            select new { endpoint.Name, FloorName = floor.Name, floor.FloorNumber };

        var totalMatches = await areas.CountAsync(cancellationToken);
        var results = await areas
            .OrderBy(area => area.Name)
            .ThenBy(area => area.FloorNumber)
            .Take(limit)
            .Select(area => new VoiceAreaSearchItem(area.Name, area.FloorName, area.FloorNumber))
            .ToListAsync(cancellationToken);

        return new VoiceAreaSearchResponse(totalMatches, results);
    }
}
