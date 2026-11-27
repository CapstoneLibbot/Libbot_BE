
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record RobotWorkflowDocument(
    int Version,
    Guid WorkflowId,
    string Name,
    string Type,
    string RuntimeId,
    IReadOnlyList<RobotWorkflowTaskDocument> Tasks)
{
    public IReadOnlyList<JsonElement> Steps => Tasks
        .OrderBy(task => task.Sequence)
        .SelectMany(task => task.Steps.OrderBy(step => step.Sequence))
        .Select(step => step.Configuration)
        .ToArray();
}

public sealed record RobotWorkflowTaskDocument(
    Guid WorkflowTaskId,
    int Sequence,
    string Name,
    string? Description,
    IReadOnlyList<RobotWorkflowStepDocument> Steps);

public sealed record RobotWorkflowStepDocument(
    Guid TaskStepId,
    int Sequence,
    string Type,
    JsonElement Configuration);
