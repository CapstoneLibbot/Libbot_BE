
using System.ComponentModel.DataAnnotations;

namespace LiBot.Application.Models;

public sealed record NavigationGoalResponse(string RuntimeId, string CommandId, string Status);
