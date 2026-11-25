namespace LiBot.Application.Models;

public sealed record WorkflowTaskResponse(
    Guid Id,
    int Sequence,
    string Name,
    string? Description,
    IReadOnlyList<WorkflowStepResponse> Steps);
