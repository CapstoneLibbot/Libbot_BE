using LiBot.Application.Models.ShelfLevel;

namespace LiBot.Application.Models.Shelf;

public class ShelfResponse
{
    public ShelfResponse(Guid id, Guid aisleId, Guid startEndpointId, Guid endEndpointId, string name,
        string? description, string status, ShelfAisleResponse? aisle = null,
        IReadOnlyList<ShelfLevelResponse>? levels = null)
        : this(id, aisleId, name, name, default, null,
            startEndpointId == Guid.Empty ? null : startEndpointId,
            description, status, aisle, levels)
    {
        EndEndpointId = endEndpointId;
    }

    public ShelfResponse(Guid id, Guid aisleId, string code, string name, DateTime createdAtUtc,
        DateTime? updatedAtUtc, Guid? endpointId, string? description, string status,
        ShelfAisleResponse? aisle = null, IReadOnlyList<ShelfLevelResponse>? levels = null)
    {
        Id = id; AisleId = aisleId; Code = code; Name = name; CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc; EndpointId = endpointId; Description = description; Status = status;
        StartEndpointId = endpointId ?? Guid.Empty; EndEndpointId = endpointId ?? Guid.Empty;
        Aisle = aisle; Levels = levels;
    }

    public Guid Id { get; }
    public Guid AisleId { get; }
    public Guid StartEndpointId { get; }
    public Guid EndEndpointId { get; private set; }
    public Guid? EndpointId { get; }
    public string Code { get; } = string.Empty;
    public string Name { get; }
    public string? Description { get; }
    public string Status { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
    public ShelfAisleResponse? Aisle { get; }
    public IReadOnlyList<ShelfLevelResponse>? Levels { get; }
}
