
using System.Text.Json;

namespace LiBot.Application.Models;

public record WorkflowResponse(Guid Id, string Name, string? Description, string Type, bool IsActive,
    IReadOnlyList<WorkflowStepResponse> Steps);
