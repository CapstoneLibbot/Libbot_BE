using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityFeedbackRequest(Guid TaskId, int Rating, string? Comment = null);
