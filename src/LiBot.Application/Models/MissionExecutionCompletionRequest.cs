
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionExecutionCompletionRequest(string Status, string? ExecutionId = null, string? Message = null);
