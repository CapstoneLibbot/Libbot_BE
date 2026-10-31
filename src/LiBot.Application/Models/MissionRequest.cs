
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionRequest(string? WorkflowId, string? RobotId, string? Priority, string? Name = null,
    string? Description = null, string? RuntimeId = null, JsonElement? InitialContext = null);
