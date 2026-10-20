using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityFeedbackResponse(Guid FeedbackId, Guid TaskId, int Rating);
