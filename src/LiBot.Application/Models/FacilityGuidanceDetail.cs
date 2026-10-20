using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityGuidanceDetail(
    Guid FacilityId,
    string Name,
    string Type,
    string? Description,
    FacilityLocation Location,
    IReadOnlyList<string> Actions);
