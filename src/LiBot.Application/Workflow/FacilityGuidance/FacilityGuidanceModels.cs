using System.Text.Json;

namespace LiBot.Application.Workflow.FacilityGuidance;

public sealed record FacilityGuidanceLoadedWorkflow(
    Guid Id,
    string Type,
    string Name,
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Steps);

public sealed record FacilityGuidanceStartResult(
    Guid MissionId,
    FacilityGuidanceLoadedWorkflow? Workflow,
    JsonElement Robot,
    bool IsAccepted);

public sealed class FacilityGuidanceWorkflowConfigurationException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);

public sealed class FacilityGuidanceRobotUnavailableException(string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException);

public sealed class FacilityGuidanceExecutionNotFoundException(string executionId)
    : InvalidOperationException($"Facility guidance execution '{executionId}' was not found or has expired.");
