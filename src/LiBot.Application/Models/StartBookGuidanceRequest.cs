using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record StartBookGuidanceRequest(
    string? RuntimeId = null,
    Guid? EndpointId = null,
    string? ArmLeaseId = null);
