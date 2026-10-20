
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record InitialPoseRequest(
    [Range(-1000, 1000)] double X,
    [Range(-1000, 1000)] double Y,
    [Range(-100, 100)] double Yaw,
    string? ArmLeaseId = null);
