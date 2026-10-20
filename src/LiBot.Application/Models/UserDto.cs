namespace LiBot.Application.Models;

public record UserDto(Guid Id, string Email, string FullName, bool IsActive);
