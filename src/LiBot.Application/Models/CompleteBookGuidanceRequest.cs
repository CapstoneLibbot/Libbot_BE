using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record CompleteBookGuidanceRequest(string Status, string? Message = null);
