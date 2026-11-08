namespace LiBot.Application.Models.Endpoint;

public class EndpointResponse(
    Guid Id,
    Guid MapId,
    string Name,
    double PositionX,
    double PositionY,
    double Theta,
    bool IsActive,
    string? Description,
    string? EndpointType,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    EndpointMapResponse? Map = null)
{
    public Guid Id { get; } = Id;
    public Guid MapId { get; } = MapId;
    public string Name { get; } = Name;
    public double PositionX { get; } = PositionX;
    public double PositionY { get; } = PositionY;
    public double Theta { get; } = Theta;
    public bool IsActive { get; } = IsActive;
    public string? Description { get; } = Description;
    public string? EndpointType { get; } = EndpointType;
    public DateTime CreatedAtUtc { get; } = CreatedAtUtc;
    public DateTime? UpdatedAtUtc { get; } = UpdatedAtUtc;
    public EndpointMapResponse? Map { get; } = Map;
}
