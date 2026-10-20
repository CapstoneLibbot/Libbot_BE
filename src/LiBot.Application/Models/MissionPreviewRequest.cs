
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionPreviewRequest(string WorkflowId, string? RobotId = null, string? MapId = null);
