
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionResponse(Guid Id, Guid WorkflowId, string WorkflowName, string? Description, string Status,
    string Priority, DateTime CreatedAt, DateTime? StartedAt, DateTime? CompletedAt, Guid? RobotId, string? RobotName,
    string? RuntimeId = null);
