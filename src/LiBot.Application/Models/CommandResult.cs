
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record CommandResult(bool Success, string? Error, JsonElement? Payload, string? CommandId)
{
    public static CommandResult Ok(JsonElement payload) => new(true, null, payload, null);
    public static CommandResult Fail(string error) => new(false, error, null, null);
}
