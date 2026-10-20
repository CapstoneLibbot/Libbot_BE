
using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record PatrolObservationRequest(Guid MissionId, string RuntimeId, JsonElement Observation);
