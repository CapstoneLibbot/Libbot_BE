using LiBot.Application.Robot.Commands;

namespace LiBot.Application.Robot.Handlers;

public class SendRobotCommandHandler
{
    public string Handle(SendRobotCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.RobotId))
            throw new ArgumentException("RobotId is required.", nameof(command));

        return $"Robot {command.RobotId} executed action: {command.Action}";
    }
}
