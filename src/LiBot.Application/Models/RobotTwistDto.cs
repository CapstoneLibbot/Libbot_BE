
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record RobotTwistDto(double LinearX, double LinearY, double AngularZ);
