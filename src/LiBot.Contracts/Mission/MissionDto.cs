namespace LiBot.Contracts.Mission;

public record MissionDto(Guid Id, string Name, string Description, string Status, DateTime CreatedAtUtc);
