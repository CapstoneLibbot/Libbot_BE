using LiBot.Domain.Workflow;

namespace LiBot.Application.Workflow;

public sealed record WorkflowDocumentSource(
    WorkflowDefinition Workflow,
    IReadOnlyList<WorkflowTask> Tasks,
    IReadOnlyList<TaskStep> Steps);
