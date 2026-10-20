
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record InventoryFinalizeRequest(Guid MissionId, string RuntimeId, string? ShelfCode = null,
    string? ScopeType = null, string? ReportType = null);
