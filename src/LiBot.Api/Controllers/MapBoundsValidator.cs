using System.Text.Json;
using LiBot.Application.Models;

namespace LiBot.Api.Controllers;

internal static class MapBoundsValidator
{
    public static bool Contains(MapResponse map, double x, double y)
    {
        if (!TryGetGrid(map, out var resolution, out var originX, out var originY, out var originYaw))
            return x >= 0 && y >= 0 && x <= map.Width && y <= map.Height;
        var cosine = Math.Cos(originYaw);
        var sine = Math.Sin(originYaw);
        var dx = x - originX;
        var dy = y - originY;
        var localX = cosine * dx + sine * dy;
        var localY = -sine * dx + cosine * dy;
        return localX >= 0 && localY >= 0
            && localX <= map.Width * resolution
            && localY <= map.Height * resolution;
    }

    public static bool Contains(MapResponse map, double minX, double minY, double maxX, double maxY)
    {
        if (!TryGetGrid(map, out var resolution, out var originX, out var originY, out var originYaw))
            return minX >= 0 && minY >= 0 && maxX <= map.Width && maxY <= map.Height;
        var cosine = Math.Cos(originYaw);
        var sine = Math.Sin(originYaw);
        var corners = new[]
        {
            (0d, 0d), (map.Width * resolution, 0d),
            (0d, map.Height * resolution), (map.Width * resolution, map.Height * resolution)
        }.Select(point => (
            X: originX + cosine * point.Item1 - sine * point.Item2,
            Y: originY + sine * point.Item1 + cosine * point.Item2)).ToArray();
        return minX >= corners.Min(point => point.X) && maxX <= corners.Max(point => point.X)
            && minY >= corners.Min(point => point.Y) && maxY <= corners.Max(point => point.Y);
    }

    private static bool TryGetGrid(MapResponse map, out double resolution, out double originX, out double originY, out double originYaw)
    {
        resolution = 0;
        originX = originY = originYaw = 0;
        if (map.Data is not { } data || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty("resolution", out var resolutionValue)
            || !resolutionValue.TryGetDouble(out resolution)
            || !double.IsFinite(resolution)
            || resolution <= 0
            || !data.TryGetProperty("origin", out var origin)
            || origin.ValueKind != JsonValueKind.Array
            || origin.GetArrayLength() < 3)
            return false;

        var values = origin.EnumerateArray().Take(3).ToArray();
        return values[0].TryGetDouble(out originX)
            && values[1].TryGetDouble(out originY)
            && values[2].TryGetDouble(out originYaw)
            && double.IsFinite(originX)
            && double.IsFinite(originY)
            && double.IsFinite(originYaw);
    }
}
