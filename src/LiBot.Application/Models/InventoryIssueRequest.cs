namespace LiBot.Application.Models;

public sealed record InventoryIssueRequest(
    Guid MissionId,
    string RuntimeId,
    string IssueType,
    string Barcode,
    DateTimeOffset ScannedAt);
