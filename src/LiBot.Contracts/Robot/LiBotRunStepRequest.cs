using System.Text.Json.Serialization;

namespace LiBot.Contracts.Robot;

public sealed record LiBotRunStepRequest(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("step")] LiBotMoveStep Step);
