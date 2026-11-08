using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Endpoint : Entity
{
    public Guid MapId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? EndpointType { get; private set; }
    public double PositionX { get; private set; }
    public double PositionY { get; private set; }
    public double Theta { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Endpoint(Guid mapId, string name, double positionX, double positionY, double theta = 0,
        string? description = null, string? endpointType = null)
    {
        if (mapId == Guid.Empty)
            throw new ArgumentException("Map id is required.", nameof(mapId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Endpoint name is required.", nameof(name));

        MapId = mapId;
        Name = name.Trim();
        Description = description?.Trim();
        EndpointType = endpointType?.Trim();
        PositionX = positionX;
        PositionY = positionY;
        Theta = theta;
    }

    public void Deactivate() => IsActive = false;

    public void Update(Guid mapId, string name, double positionX, double positionY, double theta, bool isActive,
        string? description = null, string? endpointType = null)
    {
        if (mapId == Guid.Empty)
            throw new ArgumentException("Map id is required.", nameof(mapId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Endpoint name is required.", nameof(name));
        if (!double.IsFinite(positionX) || !double.IsFinite(positionY) || !double.IsFinite(theta))
            throw new ArgumentException("Endpoint coordinates must be finite numbers.");

        MapId = mapId;
        Name = name.Trim();
        Description = description?.Trim();
        EndpointType = endpointType?.Trim();
        PositionX = positionX;
        PositionY = positionY;
        Theta = theta;
        IsActive = isActive;
        SetUpdated();
    }
}
