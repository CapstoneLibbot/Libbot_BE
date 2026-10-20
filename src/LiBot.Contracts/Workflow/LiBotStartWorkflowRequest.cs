using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Contracts.Workflow;

public sealed record LiBotStartWorkflowRequest(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("execution_id")] string ExecutionId,
    [property: JsonPropertyName("task_id")] string TaskId,
    [property: JsonPropertyName("initial_context")] IReadOnlyDictionary<string, object?> InitialContext,
    [property: JsonPropertyName("steps")] IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps);
