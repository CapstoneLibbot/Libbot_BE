namespace LiBot.Contracts.Robot;

public record RobotDto(Guid Id, string Name, string SerialNumber, string Status, double BatteryLevel);
