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
            from area in db.Areas.AsNoTracking()
            join floor in db.Floors.AsNoTracking() on area.FloorId equals floor.Id
            where area.Name.ToLower().Contains(term)
                  || (area.Description != null && area.Description.ToLower().Contains(term))
            select new { area.Name, FloorName = floor.Name, floor.FloorNumber };

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
