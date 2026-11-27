using LiBot.Application.Models;
using LiBot.Application.Workflow;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<IReadOnlyList<WorkflowAggregate>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkflowAggregate?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> CreateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowTaskWriteRequest> tasks, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowTaskWriteRequest> tasks, CancellationToken cancellationToken);
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> UpdateStatusAsync(Guid id, LiBot.Domain.Enums.WorkflowStatus status, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
