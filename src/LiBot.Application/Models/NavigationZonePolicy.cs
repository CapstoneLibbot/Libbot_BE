namespace LiBot.Application.Models;

public sealed record NavigationZonePolicy(
    string ZoneType,
    double MinX,
    double MinY,
    double MaxX,
    double MaxY,
    double MaxSpeed);
