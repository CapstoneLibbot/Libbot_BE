
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record InventoryScanRequest(
    Guid JobId,
    string RuntimeId,
    string? Barcode = null,
    string? ShelfCode = null,
    int? LevelNumber = null,
    string? QrCode = null)
{
    public string? Code => string.IsNullOrWhiteSpace(QrCode) ? Barcode : QrCode;
}
