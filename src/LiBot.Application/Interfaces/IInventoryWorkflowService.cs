using LiBot.Application.Models;

namespace LiBot.Application.Interfaces;

public interface IInventoryWorkflowService
{
    Task<InventoryWorkflowActionResponse> ExecuteAsync(
        InventoryWorkflowActionRequest request,
        bool simulation,
        CancellationToken cancellationToken);

    Task<InventoryWorkflowReportResponse?> GetReportAsync(
        Guid missionId,
        string runtimeId,
        CancellationToken cancellationToken);
}
