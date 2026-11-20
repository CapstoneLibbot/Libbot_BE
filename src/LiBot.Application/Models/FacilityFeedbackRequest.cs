using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityFeedbackRequest(Guid JobId, int Rating, string? Comment = null);
