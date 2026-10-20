
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record RobotWorkflowDocument(
    int Version,
    Guid WorkflowId,
    string Name,
    string Type,
    string RuntimeId,
    IReadOnlyList<JsonElement> Steps);
