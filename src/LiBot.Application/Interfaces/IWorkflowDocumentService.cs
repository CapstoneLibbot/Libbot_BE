using LiBot.Application.Models;
using LiBot.Application.Workflow;

namespace LiBot.Application.Services.Workflow;

public interface IWorkflowDocumentService
{
    IReadOnlyList<WorkflowTemplateResponse> GetTemplates();
    WorkflowTemplateResponse? GetTemplate(string templateId);
    string? ValidateSteps(IReadOnlyList<WorkflowStepWriteRequest> steps);
    Task<RobotWorkflowDocument?> BuildRobotDocumentAsync(Guid workflowId, string runtimeId, CancellationToken cancellationToken);
}
