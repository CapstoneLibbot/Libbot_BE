using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityLocation(
    Guid MapId,
    string MapName,
    int MapWidth,
    int MapHeight,
    FacilityGuidanceEndpoint Endpoint);
