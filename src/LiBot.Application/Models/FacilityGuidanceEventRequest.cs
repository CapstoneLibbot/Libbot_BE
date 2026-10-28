using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityGuidanceEventRequest(
    string EventType,
    JsonElement? Data = null,
    string? ExecutionId = null);
