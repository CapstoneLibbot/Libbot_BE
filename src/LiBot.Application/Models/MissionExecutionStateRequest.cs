
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionExecutionStateRequest(string? ExecutionId, string? RuntimeId, string Status,
    string? StepId = null, string? StepName = null, string? StepType = null,
    string? ExpectedEventType = null, IReadOnlyList<string>? AllowedValues = null,
    string? ExpectedBookId = null, JsonElement? Result = null, string? Error = null);
