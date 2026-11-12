namespace LiBot.Domain.Robot;

public class Robot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid MapId { get; private set; }
    public string Name { get; private set; }
    public string SerialNumber { get; private set; }
    public string Status { get; private set; } = "Offline";
    public double BatteryLevel { get; private set; }

    public Robot(string name, string serialNumber, Guid mapId = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Robot name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(serialNumber))
            throw new ArgumentException("Serial number is required.", nameof(serialNumber));

        Name = name.Trim();
        SerialNumber = serialNumber.Trim();
        MapId = mapId;
    }

    public void UpdateStatus(string status)
    {
        Status = status;
    }

    public void UpdateBattery(double batteryLevel)
    {
        BatteryLevel = Math.Clamp(batteryLevel, 0, 100);
    }
}
