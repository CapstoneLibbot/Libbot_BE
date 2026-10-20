
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record OccupancyGridDto(
    DateTimeOffset Timestamp,
    uint Width,
    uint Height,
    double Resolution,
    double OriginX,
    double OriginY,
    double OriginYaw,
    int[] Data);
