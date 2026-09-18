using LiBot.Application.Identity.Commands;
using LiBot.Domain.Identity;

namespace LiBot.Application.Identity.Handlers;

public class CreateUserCommandHandler
{
    public User Handle(CreateUserCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
            throw new ArgumentException("Email is required.", nameof(command));

        return new User(command.Email, command.FullName);
    }
}
