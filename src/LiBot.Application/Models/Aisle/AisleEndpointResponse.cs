namespace LiBot.Application.Models.Aisle;

public class AisleEndpointResponse(
    Guid Id,
    Guid MapId,
    string Name,
    double PositionX,
    double PositionY,
    double Theta,
    bool IsActive)
{
    public Guid Id { get; } = Id;
    public Guid MapId { get; } = MapId;
    public string Name { get; } = Name;
    public double PositionX { get; } = PositionX;
    public double PositionY { get; } = PositionY;
    public double Theta { get; } = Theta;
    public bool IsActive { get; } = IsActive;
}
