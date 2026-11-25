
using System.Text.Json;

namespace LiBot.Application.Models;

public record JobStartRequest(string? ArmLeaseId = null, string? RuntimeId = null);
