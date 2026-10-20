
using System.Text.Json;

namespace LiBot.Application.Models;

public record WorkflowWriteRequest(string Name, string? Description, string? Type, bool? IsActive,
    IReadOnlyList<WorkflowStepWriteRequest>? Steps);
