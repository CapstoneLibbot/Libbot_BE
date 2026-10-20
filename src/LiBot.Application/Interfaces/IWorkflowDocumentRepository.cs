using LiBot.Application.Workflow;
using LiBot.Domain.Library;

namespace LiBot.Application.Services.Workflow;

public interface IWorkflowDocumentRepository
{
    Task<WorkflowDocumentSource?> GetDocumentSourceAsync(Guid workflowId, CancellationToken cancellationToken);
    Task<Endpoint?> GetActiveEndpointAsync(Guid? endpointId, string? endpointName, CancellationToken cancellationToken);
}
