using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Endpoint : Entity
{
    public static readonly IReadOnlySet<string> SupportedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "WAYPOINT",
        "CHARGER",
        "PARK"
    };

    public Guid MapId { get; private set; }
    public string Name { get; private set; }
    public string EndpointType { get; private set; }
    public string? Description { get; private set; }
    public double PositionX { get; private set; }
    public double PositionY { get; private set; }
    public double Theta { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Endpoint(Guid mapId, string name, double positionX, double positionY, double theta = 0,
        string? description = null, string endpointType = "WAYPOINT")
    {
        if (mapId == Guid.Empty)
            throw new ArgumentException("Map id is required.", nameof(mapId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Endpoint name is required.", nameof(name));
        if (!double.IsFinite(positionX) || !double.IsFinite(positionY) || !double.IsFinite(theta))
            throw new ArgumentException("Endpoint coordinates and heading must be finite.");
        if (string.IsNullOrWhiteSpace(endpointType) || !SupportedTypes.Contains(endpointType.Trim()))
            throw new ArgumentException($"Unsupported endpoint type '{endpointType}'.", nameof(endpointType));

        MapId = mapId;
        Name = name.Trim();
        EndpointType = endpointType.Trim().ToUpperInvariant();
        Description = description?.Trim();
        PositionX = positionX;
        PositionY = positionY;
        Theta = theta;
    }

    public void Deactivate() => IsActive = false;

    public void UpdatePosition(double positionX, double positionY, double theta)
    {
        if (!double.IsFinite(positionX) || !double.IsFinite(positionY) || !double.IsFinite(theta))
            throw new ArgumentException("Endpoint coordinates and heading must be finite.");

        PositionX = positionX;
        PositionY = positionY;
        Theta = theta;
    }

    public void Update(Guid mapId, string name, double positionX, double positionY, double theta,
        string? description, string endpointType, bool isActive)
    {
        var updated = new Endpoint(mapId, name, positionX, positionY, theta, description, endpointType);
        MapId = updated.MapId;
        Name = updated.Name;
        EndpointType = updated.EndpointType;
        Description = updated.Description;
        PositionX = updated.PositionX;
        PositionY = updated.PositionY;
        Theta = updated.Theta;
        IsActive = isActive;
        SetUpdated();
    }
}
