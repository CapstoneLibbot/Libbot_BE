
using System.Text.Json;

namespace LiBot.Application.Models;

public record MapWriteRequest(string Name, int Width, int Height, JsonElement? Data, string? Version = null,
    Guid? FloorId = null, string? Status = null);
