
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record NavigationGoalRequest(
    [Range(-1000, 1000)] double X,
    [Range(-1000, 1000)] double Y,
    [Range(-100, 100)] double Yaw,
    string? ArmLeaseId,
    string SpeedMode = "NORMAL",
    string ObstaclePolicy = "REROUTE",
    [Range(1, 600)] int TimeoutSec = 180);
