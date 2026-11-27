
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record PatrolObservationRequest(Guid JobId, string RuntimeId, JsonElement Observation);
