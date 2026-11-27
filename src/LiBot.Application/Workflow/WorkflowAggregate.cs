using LiBot.Domain.Workflow;

namespace LiBot.Application.Workflow;

public sealed record WorkflowAggregate(
    WorkflowDefinition Workflow,
    IReadOnlyList<WorkflowTask> Tasks,
    IReadOnlyDictionary<Guid, IReadOnlyList<TaskStep>> Steps);
