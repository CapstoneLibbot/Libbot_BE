using LiBot.Domain.Common;

namespace LiBot.Domain.Robot;

public class RobotLog : Entity
{
    public Guid RobotId { get; private set; }
    public Guid MapId { get; private set; }
    public double CurrentBatteryLevel { get; private set; }
    public double PositionX { get; private set; }
    public double PositionY { get; private set; }
    public string Status { get; private set; } = "UNKNOWN";
    public string? Data { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("RecordAt")]
    public DateTime RecordedAtUtc { get; private set; }

    private RobotLog() { }

    public RobotLog(Guid robotId, Guid mapId, double currentBatteryLevel, double positionX, double positionY, double yaw = 0,
        DateTime recordedAtUtc = default, string status = "UNKNOWN", string? data = null)
    {
        if (robotId == Guid.Empty)
            throw new ArgumentException("Robot id is required.", nameof(robotId));
        if (mapId == Guid.Empty)
            throw new ArgumentException("Map id is required.", nameof(mapId));
        if (currentBatteryLevel is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(currentBatteryLevel));

        RobotId = robotId;
        MapId = mapId;
        CurrentBatteryLevel = currentBatteryLevel;
        PositionX = positionX;
        PositionY = positionY;
        Status = string.IsNullOrWhiteSpace(status) ? "UNKNOWN" : status.Trim();
        Data = data;
        RecordedAtUtc = recordedAtUtc == default ? DateTime.UtcNow : recordedAtUtc;
    }
}
