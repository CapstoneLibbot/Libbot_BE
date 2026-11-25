
using System.Text.Json;

namespace LiBot.Application.Models;

public record JobExecutionCompletionRequest(string Status, string? ExecutionId = null, string? Message = null);
