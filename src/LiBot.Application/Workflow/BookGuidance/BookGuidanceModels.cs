using System.Text.Json;

namespace LiBot.Application.Workflow.BookGuidance;

public sealed record BookGuidanceLoadedWorkflow(
    Guid Id,
    string Type,
    string Name,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps);

public sealed record BookGuidanceStartResult(
    Guid MissionId,
    BookGuidanceLoadedWorkflow? Workflow,
    JsonElement Robot,
    bool IsAccepted);

public sealed class BookGuidanceWorkflowConfigurationException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);

public sealed class BookGuidanceRobotUnavailableException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);
