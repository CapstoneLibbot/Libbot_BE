using LiBot.Application.Models;
using LiBot.Application.Workflow;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Interfaces;

public interface IWorkflowRepository
{
    Task<IReadOnlyList<WorkflowAggregate>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkflowAggregate?> GetByIdAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<bool> CreateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowStepWriteRequest> steps, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(WorkflowDefinition workflow, IReadOnlyList<WorkflowStepWriteRequest> steps, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
