using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record StartBookGuidanceRequest(
    string? Query = null,
    Guid RobotId = default,
    string? BackendBaseUrl = null,
    string? RuntimeId = null,
    Guid? BookId = null,
    Guid? BookItemId = null,
    string? ArmLeaseId = null,
    Guid? EndpointId = null);
