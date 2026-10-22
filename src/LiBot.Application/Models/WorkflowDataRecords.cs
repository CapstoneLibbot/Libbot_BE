namespace LiBot.Application.Models;

public sealed record SimulationEndpointRecord(Guid Id, string Name, Guid MapId, double X, double Y, double Theta);
public sealed record SimulationMapRecord(Guid Id, string Name, int Width, int Height);
public sealed record WorkflowMissionRecord(string RuntimeId, DateTime? StartedAtUtc);
public sealed record InventoryBookRecord(Guid? ExpectedShelfLevelId, string? ExpectedShelfCode);
