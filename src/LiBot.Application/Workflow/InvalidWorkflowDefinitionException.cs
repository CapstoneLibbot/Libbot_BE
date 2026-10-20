namespace LiBot.Application.Workflow;

public sealed class InvalidWorkflowDefinitionException(string message, Exception? inner = null) : Exception(message, inner);
