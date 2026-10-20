
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record WorkflowTemplateStep(
    int Sequence,
    string Type,
    string Name,
    JsonElement Configuration);
