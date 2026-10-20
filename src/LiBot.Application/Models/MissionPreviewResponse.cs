
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionPreviewResponse(Guid WorkflowId, Guid? RobotId, Guid? MapId, IReadOnlyList<Point> Path,
    IReadOnlyList<WorkflowStepResponse> Steps, double EstimatedDistance, int EstimatedDuration);
