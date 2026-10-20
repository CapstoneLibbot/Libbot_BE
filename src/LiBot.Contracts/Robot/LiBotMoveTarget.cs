using System.Text.Json.Serialization;

namespace LiBot.Contracts.Robot;

public sealed record LiBotMoveTarget(
    [property: JsonPropertyName("endpoint_id")] string EndpointId,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y,
    [property: JsonPropertyName("theta")] double Theta);
