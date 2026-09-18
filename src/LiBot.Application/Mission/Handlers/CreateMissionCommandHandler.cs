using LiBot.Application.Mission.Commands;
using DomainMission = LiBot.Domain.Mission.Mission;

namespace LiBot.Application.Mission.Handlers;

public class CreateMissionCommandHandler
{
    public DomainMission Handle(CreateMissionCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new ArgumentException("Mission name is required.", nameof(command));

        return new DomainMission(command.Name, command.Description);
    }
}
