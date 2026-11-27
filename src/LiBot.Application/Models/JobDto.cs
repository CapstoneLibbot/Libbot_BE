namespace LiBot.Application.Models;

public record JobDto(Guid Id, string Name, string Description, string Status, DateTime CreatedAtUtc);
