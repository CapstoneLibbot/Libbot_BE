using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record FeedbackRequest(Guid TaskId, int Rating, string? Comment = null);
