namespace LiBot.Application.Models;

/// <summary>Minimal projection used to validate a specific book copy when starting book guidance.</summary>
public sealed record BookCopyStartTarget(
    string BookTitle,
    string? CopyStatus,
    NavigationEndpoint? Endpoint);
