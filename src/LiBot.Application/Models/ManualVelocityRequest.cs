
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record ManualVelocityRequest(
    [Range(-0.7, 0.7)] double LinearX,
    [Range(-0.7, 0.7)] double LinearY,
    [Range(-0.5, 0.5)] double AngularZ,
    string? ArmLeaseId);
