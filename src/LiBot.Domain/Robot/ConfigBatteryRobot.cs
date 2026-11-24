using LiBot.Domain.Common;

namespace LiBot.Domain.Robot;

public sealed class ConfigBatteryRobot : Entity
{
    public Guid AccountId { get; private set; }
    public Guid RobotId { get; private set; }
    public double BatteryThreshold { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ConfigBatteryRobot() { }

    public ConfigBatteryRobot(Guid accountId, Guid robotId, double batteryThreshold, bool isActive = true)
    {
        if (accountId == Guid.Empty || robotId == Guid.Empty)
            throw new ArgumentException("Account and robot ids are required.");
        if (!double.IsFinite(batteryThreshold) || batteryThreshold is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(batteryThreshold));

        AccountId = accountId;
        RobotId = robotId;
        BatteryThreshold = batteryThreshold;
        IsActive = isActive;
    }
}
