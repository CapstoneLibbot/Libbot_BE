using LiBot.Application.Models;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Interfaces;

public interface IScheduleService
{
    Task<IReadOnlyList<ScheduleResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<ScheduleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ScheduleResponse> CreateAsync(ScheduleWriteRequest request, CancellationToken cancellationToken);
    Task<ScheduleResponse?> UpdateAsync(Guid id, ScheduleWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
