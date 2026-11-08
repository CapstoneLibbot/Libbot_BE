using LiBot.Domain.Common;

namespace LiBot.Domain.Robot;

public class RobotLog : Entity
{
    public Guid RobotId { get; private set; }
    public Guid? MapId { get; private set; }
    public double? PositionX { get; private set; }
    public double? PositionY { get; private set; }
    public double CurrentBatteryLevel { get; private set; }
    public string? Status { get; private set; }
    public string? Data { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    public RobotLog(Guid robotId, double currentBatteryLevel, DateTime recordedAtUtc = default,
        Guid? mapId = null, double? positionX = null, double? positionY = null, string? status = null, string? data = null)
    {
        if (robotId == Guid.Empty)
            throw new ArgumentException("Robot id is required.", nameof(robotId));
        if (currentBatteryLevel is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(currentBatteryLevel));

        RobotId = robotId;
        CurrentBatteryLevel = currentBatteryLevel;
        RecordedAtUtc = recordedAtUtc == default ? DateTime.UtcNow : recordedAtUtc;
        MapId = mapId;
        PositionX = positionX;
        PositionY = positionY;
        Status = status?.Trim();
        Data = data;
    }
}
