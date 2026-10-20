using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow;
using LiBot.Domain.Library;
using LiBot.Domain.Workflow;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Infrastructure.Workflow;

public sealed class WorkflowDocumentRepository(LiBotDbContext db) : IWorkflowDocumentRepository
{
    public async Task<WorkflowDocumentSource?> GetDocumentSourceAsync(
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        var workflow = await db.Workflows.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == workflowId, cancellationToken);
        if (workflow is null) return null;

        var tasks = await db.WorkflowTasks.AsNoTracking()
            .Where(item => item.WorkflowId == workflowId)
            .OrderBy(item => item.SequenceNumber)
            .ToListAsync(cancellationToken);
        var taskIds = tasks.Select(item => item.Id).ToList();
        var steps = await db.TaskSteps.AsNoTracking()
            .Where(item => taskIds.Contains(item.WorkflowTaskId))
            .ToListAsync(cancellationToken);

        return new WorkflowDocumentSource(workflow, tasks, steps);
    }

    public Task<Endpoint?> GetActiveEndpointAsync(
        Guid? endpointId,
        string? endpointName,
        CancellationToken cancellationToken)
    {
        var query = db.Endpoints.AsNoTracking().Where(item => item.IsActive);
        return endpointId is Guid id
            ? query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            : query.FirstOrDefaultAsync(item => item.Name == endpointName, cancellationToken);
    }
}
