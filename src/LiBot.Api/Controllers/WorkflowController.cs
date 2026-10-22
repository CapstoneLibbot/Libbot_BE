using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow;
using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Quản lý workflow và xuất cấu hình workflow cho runtime robot.</summary>
[ApiController]
[Route("api/workflows")]
public class WorkflowController(
    IWorkflowManagementService workflows,
    IWorkflowDocumentService documents) : ControllerBase
{
    /// <summary>Lấy danh sách mẫu workflow dựng sẵn.</summary>
    [HttpGet("templates")]
    public ActionResult<IReadOnlyList<WorkflowTemplateResponse>> GetTemplates() => Ok(workflows.GetTemplates());

    /// <summary>Tạo workflow mới từ một mẫu.</summary>
    [HttpPost("templates/{templateId}/create")]
    public async Task<ActionResult<WorkflowResponse>> CreateFromTemplate(
        string templateId, WorkflowTemplateCreateRequest? request, CancellationToken cancellationToken)
    {
        var result = await workflows.CreateFromTemplateAsync(templateId, request, cancellationToken);
        if (result.Status == WorkflowOperationStatus.NotFound) return NotFound(new { message = result.Message });
        if (result.Status == WorkflowOperationStatus.Invalid) return BadRequest(new { message = result.Message });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Xuất workflow thành JSON theo cấu trúc runtime robot có thể chạy.</summary>
    [HttpGet("{id:guid}/robot-json")]
    public async Task<IActionResult> GetRobotJson(
        Guid id, [FromQuery] string runtimeId = "real", CancellationToken cancellationToken = default)
    {
        if (runtimeId is not ("sim" or "real"))
            return BadRequest(new { message = "runtimeId must be 'sim' or 'real'." });
        try
        {
            var document = await documents.BuildRobotDocumentAsync(id, runtimeId, cancellationToken);
            return document is null ? NotFound() : Ok(document);
        }
        catch (InvalidWorkflowDefinitionException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// <summary>Lấy danh sách workflow.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkflowResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await workflows.GetAllAsync(cancellationToken));

    /// <summary>Lấy chi tiết workflow, gồm task và step.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkflowResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var workflow = await workflows.GetByIdAsync(id, cancellationToken);
        return workflow is null ? NotFound() : Ok(workflow);
    }

    /// <summary>Tạo workflow mới.</summary>
    [HttpPost]
    public async Task<ActionResult<WorkflowResponse>> Create(WorkflowWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await workflows.CreateAsync(request, cancellationToken);
        if (result.Status == WorkflowOperationStatus.Invalid) return BadRequest(new { message = result.Message });
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Cập nhật workflow theo ID.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<WorkflowResponse>> Update(Guid id, WorkflowWriteRequest request, CancellationToken cancellationToken)
    {
        var result = await workflows.UpdateAsync(id, request, cancellationToken);
        return result.Status switch
        {
            WorkflowOperationStatus.NotFound => NotFound(),
            WorkflowOperationStatus.Conflict => Conflict(result.Message),
            WorkflowOperationStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => Ok(result.Value)
        };
    }

    /// <summary>Xóa workflow nếu workflow không còn ràng buộc.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await workflows.DeleteAsync(id, cancellationToken);
        return result.Status switch
        {
            WorkflowOperationStatus.NotFound => NotFound(),
            WorkflowOperationStatus.Conflict => Conflict(result.Message),
            _ => NoContent()
        };
    }
}
