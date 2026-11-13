using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record StartFacilityGuidanceRequest(
    string Query,
    Guid RobotId,
    string? BackendBaseUrl = null,
    string? RuntimeId = null,
    Guid? FacilityId = null,
    string? ArmLeaseId = null);
