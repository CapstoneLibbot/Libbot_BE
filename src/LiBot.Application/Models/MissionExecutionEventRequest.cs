
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionExecutionEventRequest(string EventType, JsonElement? Data = null);
