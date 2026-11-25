
using System.Text.Json;

namespace LiBot.Application.Models;

public record JobUpdateRequest(string? Name, string? Description, string? Priority, string? WorkflowId = null);
