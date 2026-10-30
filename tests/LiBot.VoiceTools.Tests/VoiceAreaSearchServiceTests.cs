using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Services;
using LiBot.Domain.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LiBot.VoiceTools.Tests;

public sealed class VoiceAreaSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_matches_name_and_description_joins_floor_and_limits_to_three()
    {
        var options = new DbContextOptionsBuilder<LiBotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new LiBotDbContext(options);

        var floorOne = new Floor("Tầng một", 1);
        var floorTwo = new Floor("Tầng hai", 2);
        db.Floors.AddRange(floorOne, floorTwo);
        db.Areas.AddRange(
            new Area(floorTwo.Id, "Khu học nhóm C", "Có bàn study"),
            new Area(floorOne.Id, "Khu học nhóm A"),
            new Area(floorOne.Id, "Khu học nhóm B"),
            new Area(floorTwo.Id, "Phòng yên tĩnh", "study im lặng"));
        await db.SaveChangesAsync();

        var result = await new VoiceAreaSearchService(db).SearchAsync("study", 3, CancellationToken.None);

        Assert.Equal(2, result.TotalMatches);
        Assert.Collection(result.Areas,
            item =>
            {
                Assert.Equal("Khu học nhóm C", item.Name);
                Assert.Equal("Tầng hai", item.FloorName);
                Assert.Equal(2, item.FloorNumber);
            },
            item => Assert.Equal("Phòng yên tĩnh", item.Name));

        var byName = await new VoiceAreaSearchService(db).SearchAsync("khu học", 3, CancellationToken.None);
        Assert.Equal(3, byName.TotalMatches);
        Assert.Equal(["Khu học nhóm A", "Khu học nhóm B", "Khu học nhóm C"], byName.Areas.Select(item => item.Name));
    }

    [Fact]
    public async Task SearchAsync_returns_no_results_when_query_does_not_match()
    {
        var options = new DbContextOptionsBuilder<LiBotDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new LiBotDbContext(options);

        var result = await new VoiceAreaSearchService(db).SearchAsync("khong ton tai", 3, CancellationToken.None);

        Assert.Equal(0, result.TotalMatches);
        Assert.Empty(result.Areas);
    }
}
