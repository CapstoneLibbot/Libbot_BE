
using System.Text.Json;

namespace LiBot.Application.Models;

public record WorkflowStepWriteRequest(int Sequence, string Type, string? Name, JsonElement? Configuration, string? Id = null);
