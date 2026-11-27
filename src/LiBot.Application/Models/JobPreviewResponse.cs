
using System.Text.Json;

namespace LiBot.Application.Models;

public record JobPreviewResponse(Guid WorkflowId, Guid? RobotId, Guid? MapId, IReadOnlyList<Point> Path,
    IReadOnlyList<WorkflowStepResponse> Steps, double EstimatedDistance, int EstimatedDuration);
