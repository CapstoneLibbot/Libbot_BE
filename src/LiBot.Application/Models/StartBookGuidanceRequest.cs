using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record StartBookGuidanceRequest(
    string Query,
    Guid RobotId,
    string? BackendBaseUrl = null,
    string? RuntimeId = null,
    Guid? BookId = null,
    Guid? BookItemId = null,
    string? ArmLeaseId = null);
