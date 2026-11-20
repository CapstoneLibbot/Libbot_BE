using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record PatrolActionRequest(
    Guid MissionId,
    string RuntimeId,
    string Action,
    JsonElement Payload,
    string IdempotencyKey);
