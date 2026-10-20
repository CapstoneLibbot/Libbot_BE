using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilitySearchItem(
    Guid FacilityId,
    string Name,
    string Type,
    Guid MapId,
    string MapName);
