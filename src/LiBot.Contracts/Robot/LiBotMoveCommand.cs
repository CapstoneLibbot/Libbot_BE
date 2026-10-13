using System.Text.Json.Serialization;

namespace LiBot.Contracts.Robot;

public sealed record LiBotRunStepRequest(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("step")] LiBotMoveStep Step);

public sealed record LiBotMoveStep(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("target")] LiBotMoveTarget Target,
    [property: JsonPropertyName("speed_mode")] string SpeedMode,
    [property: JsonPropertyName("obstacle_policy")] string ObstaclePolicy,
    [property: JsonPropertyName("timeout_sec")] int TimeoutSec);

public sealed record LiBotMoveTarget(
    [property: JsonPropertyName("endpoint_id")] string EndpointId,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("theta")] double Theta);
