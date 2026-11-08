namespace LiBot.Application.Models.Endpoint;

public class EndpointWriteRequest(
    Guid MapId,
    string Name,
    double PositionX,
    double PositionY,
    double Theta = 0,
    bool IsActive = true,
    string? Description = null,
    string? EndpointType = null)
{
    public Guid MapId { get; } = MapId;
    public string Name { get; } = Name;
    public double PositionX { get; } = PositionX;
    public double PositionY { get; } = PositionY;
    public double Theta { get; } = Theta;
    public bool IsActive { get; } = IsActive;
    public string? Description { get; } = Description;
    public string? EndpointType { get; } = EndpointType;
}
