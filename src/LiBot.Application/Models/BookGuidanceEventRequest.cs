using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record BookGuidanceEventRequest(
    string EventType,
    JsonElement? Data = null,
    string? ExecutionId = null);
