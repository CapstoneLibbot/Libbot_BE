namespace LiBot.Contracts.Identity;

public record UserDto(Guid Id, string Email, string FullName, bool IsActive);
