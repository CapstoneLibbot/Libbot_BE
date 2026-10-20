using LiBot.Application.Models;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Interfaces;

public interface IScheduleService
{
    Task<IReadOnlyList<Schedule>> GetAllAsync(CancellationToken cancellationToken);
    Task<Schedule?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Schedule> CreateAsync(ScheduleWriteRequest request, CancellationToken cancellationToken);
    Task<Schedule?> UpdateAsync(Guid id, ScheduleWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
