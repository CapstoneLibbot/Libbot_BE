using LiBot.Application.Models;
using LiBot.Domain.Library;
using Xunit;

namespace LiBot.Tests.ZonePolicy;

public sealed class ZonePolicyTests
{
    private static readonly Guid MapId = Guid.NewGuid();

    [Fact]
    public void KeepOut_RoundTripsAndClearsSpeed()
    {
        var input = new ZoneDto
        {
            MapId = MapId,
            ZoneType = "keep_out",
            MinX = 1,
            MinY = 2,
            MaxX = 3,
            MaxY = 4,
            MaxSpeed = 0.8,
        };

        var entity = input.ToEntity();
        var output = new ZoneDto();
        output.LoadFrom(entity);

        Assert.Equal("KEEP_OUT", output.ZoneType);
        Assert.Equal(0, output.MaxSpeed);
    }

    [Fact]
    public void SpeedLimit_RequiresPositiveFiniteSpeed()
    {
        Assert.Throws<ArgumentException>(() => Create("SPEED_LIMIT", maxSpeed: 0));
        Assert.Throws<ArgumentException>(() => Create("SPEED_LIMIT", maxSpeed: double.NaN));

        var zone = Create("SPEED_LIMIT", maxSpeed: 0.4);
        Assert.Equal(0.4, zone.MaxSpeed);
    }

    [Fact]
    public void NoStop_ClearsUnusedParameters()
    {
        var zone = Create("NO_STOP", maxSpeed: 0.4);
        Assert.Equal(0, zone.MaxSpeed);
    }

    private static Zone Create(string type, double maxSpeed = 0) =>
        new(MapId, "test-zone", type, 0, 0, 1, 1, maxSpeed);
}
