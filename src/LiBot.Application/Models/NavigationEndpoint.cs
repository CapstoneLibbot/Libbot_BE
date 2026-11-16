using System.Text.Json;
using System.Text.Json.Serialization;

namespace LiBot.Application.Models;

public sealed record NavigationEndpoint(
    Guid EndpointId,
    Guid MapId,
    string Name,
    double X,
    double Y,
    double Theta);
