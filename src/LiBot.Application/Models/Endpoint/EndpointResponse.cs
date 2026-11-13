namespace LiBot.Application.Models.Endpoint;

public class EndpointResponse(
    Guid Id,
    Guid MapId,
    string Name,
    string? Description,
    string EndpointType,
    double PositionX,
    double PositionY,
    double Theta,
    bool IsActive,
    EndpointMapResponse? Map = null)
{
    public Guid Id { get; } = Id;
    public Guid MapId { get; } = MapId;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
    public string EndpointType { get; } = EndpointType;
    public double PositionX { get; } = PositionX;
    public double PositionY { get; } = PositionY;
    public double Theta { get; } = Theta;
    public bool IsActive { get; } = IsActive;
    public EndpointMapResponse? Map { get; } = Map;
}
