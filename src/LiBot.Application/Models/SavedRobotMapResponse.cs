
using System.Text.Json;

namespace LiBot.Application.Models;

public record SavedRobotMapResponse(Guid Id, string Name, int Width, int Height, string Version, object Data)
{
    public SavedRobotMapResponse(Guid id, string name, int width, int height, string version)
        : this(id, name, width, height, version, new { format = "occupancy-grid-v1" }) { }
}
