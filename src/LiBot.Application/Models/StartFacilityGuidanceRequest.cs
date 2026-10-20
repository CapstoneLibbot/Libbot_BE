using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record StartFacilityGuidanceRequest(string Query, string? BackendBaseUrl = null);
