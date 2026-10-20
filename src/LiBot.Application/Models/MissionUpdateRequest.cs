
using System.Text.Json;

namespace LiBot.Application.Models;

public record MissionUpdateRequest(string? Name, string? Description, string? Priority, string? WorkflowId = null,
    Guid? SourceEndpointId = null, Guid? DestinationEndpointId = null);
