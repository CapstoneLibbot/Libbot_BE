namespace LiBot.Application.Models;

public sealed record GuidanceWorkflowSnapshot(
    Guid Id,
    string Type,
    string Name,
    IReadOnlyList<GuidanceWorkflowTaskSnapshot> Tasks);

public sealed record GuidanceWorkflowTaskSnapshot(
    Guid Id,
    string Name,
    int SequenceNumber,
    IReadOnlyList<GuidanceTaskStepSnapshot> Steps);

public sealed record GuidanceTaskStepSnapshot(
    Guid Id,
    string StepType,
    string? Configuration,
    DateTime CreatedAtUtc);
