namespace LiBot.Application.Models;

public record MissionDto(Guid Id, string Name, string Description, string Status, DateTime CreatedAtUtc);
