namespace LiBot.Application.Models;

public sealed record NavigationZonePolicy(
    string ZoneType,
    double MinX,
    double MinY,
    double MaxX,
    double MaxY,
    double MaxSpeed,
    double? Width = null,
    double? Height = null,
    double RotationAngle = 0);
