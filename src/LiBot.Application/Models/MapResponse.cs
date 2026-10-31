
using System.Text.Json;

namespace LiBot.Application.Models;

public record MapResponse(Guid Id, string Name, int Width, int Height, JsonElement? Data, string Version,
    Guid? FloorId, string Status);
