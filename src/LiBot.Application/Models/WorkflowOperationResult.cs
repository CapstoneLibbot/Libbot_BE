namespace LiBot.Application.Models;

public enum WorkflowOperationStatus { Success, NotFound, Conflict, Invalid }

public sealed record WorkflowOperationResult<T>(WorkflowOperationStatus Status, T? Value = default, string? Message = null);
