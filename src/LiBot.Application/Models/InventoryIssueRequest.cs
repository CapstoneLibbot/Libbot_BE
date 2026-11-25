namespace LiBot.Application.Models;

public sealed record InventoryIssueRequest(
    Guid JobId,
    string RuntimeId,
    string IssueType,
    string Barcode,
    DateTimeOffset ScannedAt);
