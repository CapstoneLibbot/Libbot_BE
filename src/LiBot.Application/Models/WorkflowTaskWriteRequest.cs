namespace LiBot.Application.Models;

public sealed record WorkflowTaskWriteRequest(
    int Sequence,
    string Name,
    string? Description,
    IReadOnlyList<WorkflowStepWriteRequest> Steps);
