using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IWorkflowManagementService
{
    IReadOnlyList<WorkflowTemplateResponse> GetTemplates();
    WorkflowTemplateResponse? GetTemplate(string templateId);
    Task<IReadOnlyList<WorkflowResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<WorkflowResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<WorkflowOperationResult<WorkflowResponse>> CreateAsync(WorkflowWriteRequest request, CancellationToken cancellationToken);
    Task<WorkflowOperationResult<WorkflowResponse>> CreateFromTemplateAsync(string templateId, WorkflowTemplateCreateRequest? request, CancellationToken cancellationToken);
    Task<WorkflowOperationResult<WorkflowResponse>> UpdateAsync(Guid id, WorkflowWriteRequest request, CancellationToken cancellationToken);
    Task<WorkflowOperationResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
