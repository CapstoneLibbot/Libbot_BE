
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record PatrolFinalizeRequest(Guid JobId, string RuntimeId);
