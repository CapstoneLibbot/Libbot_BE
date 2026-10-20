
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record ArmRuntimeResponse(string RuntimeId, string LeaseId, DateTimeOffset ExpiresAt);
