using System.Text.Json;

namespace LiBot.Application.Models;

public sealed record FacilityGuidanceEndpoint(
    Guid EndpointId,
    string Name,
    double X,
    double Y,
    double Theta);
