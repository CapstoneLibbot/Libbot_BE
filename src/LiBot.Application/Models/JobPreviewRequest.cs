
using System.Text.Json;

namespace LiBot.Application.Models;

public record JobPreviewRequest(string WorkflowId, string? RobotId = null, string? MapId = null);
