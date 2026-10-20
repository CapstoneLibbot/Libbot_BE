
using System.Text.Json;

namespace LiBot.Application.Models;

public record WorkflowStepResponse(Guid Id, int Sequence, string Type, string Name, JsonElement? Configuration);
