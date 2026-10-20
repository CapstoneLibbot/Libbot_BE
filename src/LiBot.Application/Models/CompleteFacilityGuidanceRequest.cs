using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record CompleteFacilityGuidanceRequest(string Status, string? Message = null);
