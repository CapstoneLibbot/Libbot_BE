namespace LiBot.Application.Models;

public sealed record FeedbackUpdateRequest(int Rating, string? Comment = null);
