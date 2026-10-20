
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record InventoryScanRequest(Guid MissionId, string RuntimeId, string Barcode, string? ShelfCode = null);
