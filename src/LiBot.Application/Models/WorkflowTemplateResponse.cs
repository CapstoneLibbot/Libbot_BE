
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record WorkflowTemplateResponse(
    string Id,
    string Name,
    string Description,
    string Type,
    IReadOnlyList<WorkflowTemplateStep> Steps);
