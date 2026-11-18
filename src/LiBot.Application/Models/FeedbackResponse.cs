using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record FeedbackResponse(Guid FeedbackId, Guid JobId, int Rating, string? Comment = null);
