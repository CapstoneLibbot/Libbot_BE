using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IInventoryWorkflowService
{
    Task<InventoryWorkflowActionResponse> ExecuteAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        CancellationToken cancellationToken);

    Task<InventoryWorkflowReportResponse?> GetReportAsync(
        Guid jobId,
        string runtimeId,
        CancellationToken cancellationToken);
}
