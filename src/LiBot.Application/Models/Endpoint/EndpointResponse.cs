namespace LiBot.Application.Models.Endpoint;

public class EndpointResponse
{
    public EndpointResponse(Guid id, Guid mapId, string name, string? description, string endpointType,
        double positionX, double positionY, double theta, bool isActive, EndpointMapResponse? map = null)
        : this(id, mapId, name, positionX, positionY, theta, isActive, description, endpointType,
            default, null, map) { }

    public EndpointResponse(Guid id, Guid mapId, string name, double positionX, double positionY, double theta,
        bool isActive, string? description, string? endpointType, DateTime createdAtUtc, DateTime? updatedAtUtc,
        EndpointMapResponse? map = null)
    {
        Id = id; MapId = mapId; Name = name; Description = description; EndpointType = endpointType ?? string.Empty;
        PositionX = positionX; PositionY = positionY; Theta = theta; IsActive = isActive;
        CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc; Map = map;
    }

    public Guid Id { get; }
    public Guid MapId { get; }
    public string Name { get; }
    public string? Description { get; }
    public string EndpointType { get; }
    public double PositionX { get; }
    public double PositionY { get; }
    public double Theta { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
    public EndpointMapResponse? Map { get; }
}
