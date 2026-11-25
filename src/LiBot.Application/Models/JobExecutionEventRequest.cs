
using System.Text.Json;

namespace LiBot.Application.Models;

public record JobExecutionEventRequest(string EventType, JsonElement? Data = null);
