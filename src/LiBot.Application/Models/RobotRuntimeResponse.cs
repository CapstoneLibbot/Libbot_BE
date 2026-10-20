
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record RobotRuntimeResponse(
    string Id,
    string DisplayName,
    string Kind,
    bool Connected,
    bool Stale,
    bool Armed,
    bool MappingActive,
    bool NavigationActive,
    DateTimeOffset? ArmExpiresAt,
    DateTimeOffset? LastTelemetryAt,
    RobotTelemetryRequest? Telemetry);
