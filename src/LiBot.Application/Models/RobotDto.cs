namespace LiBot.Application.Models;

public record RobotDto(Guid Id, string Name, string SerialNumber, string Status, double BatteryLevel);
