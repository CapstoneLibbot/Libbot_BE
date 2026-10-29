using LiBot.Application.Models.Floor;

namespace LiBot.Application.Interfaces;

public interface IFloorService
{
    Task<FloorListResponse> GetAllAsync(FloorQuery query, CancellationToken cancellationToken);
    Task<FloorResponse?> GetByIdAsync(Guid id, string? expand, CancellationToken cancellationToken);
    Task<FloorResponse> CreateAsync(FloorWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<FloorResponse?> UpdateAsync(Guid id, FloorWriteRequest request, string? expand, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
