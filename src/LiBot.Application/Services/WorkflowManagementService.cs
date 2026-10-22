using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Services.Workflow;
using LiBot.Domain.Workflow;

namespace LiBot.Application.Services;

public sealed class WorkflowManagementService(
    IWorkflowRepository repository,
    IWorkflowDocumentService documents) : IWorkflowManagementService
{
    public IReadOnlyList<WorkflowTemplateResponse> GetTemplates() => documents.GetTemplates();

    public WorkflowTemplateResponse? GetTemplate(string templateId) => documents.GetTemplate(templateId);

    public async Task<IReadOnlyList<WorkflowResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(ToResponse).ToList();

    public async Task<WorkflowResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var aggregate = await repository.GetByIdAsync(id, tracking: false, cancellationToken);
        return aggregate is null ? null : ToResponse(aggregate);
    }

    public async Task<WorkflowOperationResult<WorkflowResponse>> CreateAsync(
        WorkflowWriteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Invalid<WorkflowResponse>("Workflow name is required.");
        var steps = request.Steps ?? [];
        var validationError = documents.ValidateSteps(steps);
        if (validationError is not null) return Invalid<WorkflowResponse>(validationError);

        var workflow = new WorkflowDefinition(request.Name, request.Description ?? string.Empty);
        workflow.SetType(request.Type ?? "GENERAL");
        workflow.SetActive(request.IsActive ?? true);
        await repository.CreateAsync(workflow, steps, cancellationToken);
        var aggregate = await repository.GetByIdAsync(workflow.Id, tracking: false, cancellationToken);
        return Success(ToResponse(aggregate!));
    }

    public async Task<WorkflowOperationResult<WorkflowResponse>> CreateFromTemplateAsync(
        string templateId, WorkflowTemplateCreateRequest? request, CancellationToken cancellationToken)
    {
        var template = documents.GetTemplate(templateId);
        if (template is null) return NotFound<WorkflowResponse>("Workflow template not found.");
        var steps = template.Steps.Select(step =>
            new WorkflowStepWriteRequest(step.Sequence, step.Type, step.Name, step.Configuration, step.Name)).ToList();
        var validationError = documents.ValidateSteps(steps);
        if (validationError is not null) return Invalid<WorkflowResponse>(validationError);

        var workflow = new WorkflowDefinition(
            string.IsNullOrWhiteSpace(request?.Name) ? template.Name : request.Name!, template.Description);
        workflow.SetType(template.Type);
        workflow.SetActive(true);
        await repository.CreateAsync(workflow, steps, cancellationToken);
        var aggregate = await repository.GetByIdAsync(workflow.Id, tracking: false, cancellationToken);
        return Success(ToResponse(aggregate!));
    }

    public async Task<WorkflowOperationResult<WorkflowResponse>> UpdateAsync(
        Guid id, WorkflowWriteRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Invalid<WorkflowResponse>("Workflow name is required.");
        var steps = request.Steps ?? [];
        var validationError = documents.ValidateSteps(steps);
        if (validationError is not null) return Invalid<WorkflowResponse>(validationError);
        var aggregate = await repository.GetByIdAsync(id, tracking: true, cancellationToken);
        if (aggregate is null) return NotFound<WorkflowResponse>();

        var workflow = aggregate.Workflow;
        workflow.Update(request.Name, request.Description ?? string.Empty);
        workflow.SetType(request.Type ?? workflow.Type);
        workflow.SetActive(request.IsActive ?? workflow.IsActive);
        if (!await repository.UpdateAsync(workflow, steps, cancellationToken))
            return Conflict<WorkflowResponse>("This workflow has mission history and its steps cannot be replaced.");
        var updated = await repository.GetByIdAsync(id, tracking: false, cancellationToken);
        return Success(ToResponse(updated!));
    }

    public async Task<WorkflowOperationResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        if (await repository.GetByIdAsync(id, tracking: false, cancellationToken) is null)
            return NotFound<bool>();
        if (!await repository.DeleteAsync(id, cancellationToken))
            return Conflict<bool>("This workflow has mission history and cannot be deleted.");
        return Success(true);
    }

    private static WorkflowResponse ToResponse(LiBot.Application.Workflow.WorkflowAggregate aggregate)
    {
        var steps = aggregate.Tasks
            .Where(task => aggregate.Steps.ContainsKey(task.Id))
            .Select(task =>
            {
                var step = aggregate.Steps[task.Id];
                return new WorkflowStepResponse(step.Id, task.SequenceNumber, step.StepType, task.Name,
                    ParseConfiguration(step.Configuration));
            }).ToList();
        var workflow = aggregate.Workflow;
        return new WorkflowResponse(workflow.Id, workflow.Name, workflow.Description, workflow.Type,
            workflow.IsActive, steps);
    }

    private static System.Text.Json.JsonElement? ParseConfiguration(string? configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration)) return null;
        try { return System.Text.Json.JsonDocument.Parse(configuration).RootElement.Clone(); }
        catch (System.Text.Json.JsonException) { return System.Text.Json.JsonSerializer.SerializeToElement(configuration); }
    }

    private static WorkflowOperationResult<T> Success<T>(T value) => new(WorkflowOperationStatus.Success, value);
    private static WorkflowOperationResult<T> NotFound<T>(string? message = null) => new(WorkflowOperationStatus.NotFound, Message: message);
    private static WorkflowOperationResult<T> Conflict<T>(string message) => new(WorkflowOperationStatus.Conflict, Message: message);
    private static WorkflowOperationResult<T> Invalid<T>(string message) => new(WorkflowOperationStatus.Invalid, Message: message);
}
