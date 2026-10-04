using LiBot.Domain.Common;

namespace LiBot.Domain.Robot;

public class RobotConfigLog : Entity
{
    public Guid RobotId { get; private set; }
    public Guid? AccountId { get; private set; }
    public string ConfigType { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    public RobotConfigLog(Guid robotId, string configType, Guid? accountId = null,
        string? oldValue = null, string? newValue = null)
    {
        if (robotId == Guid.Empty)
            throw new ArgumentException("Robot id is required.", nameof(robotId));
        if (string.IsNullOrWhiteSpace(configType))
            throw new ArgumentException("Configuration type is required.", nameof(configType));

        RobotId = robotId;
        AccountId = accountId;
        ConfigType = configType.Trim();
        OldValue = oldValue;
        NewValue = newValue;
    }
}