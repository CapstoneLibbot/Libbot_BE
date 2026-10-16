using System.Collections.Concurrent;

namespace LiBot.Infrastructure.Workflow;

public sealed class BookGuidanceRuntimeRegistry
{
    private readonly ConcurrentDictionary<string, string> _runtimeByExecution = new(StringComparer.Ordinal);

    public void Register(string executionId, string runtimeId) =>
        _runtimeByExecution[executionId] = runtimeId;

    public bool TryGetRuntime(string executionId, out string runtimeId) =>
        _runtimeByExecution.TryGetValue(executionId, out runtimeId!);
}

public sealed class BookGuidanceExecutionNotFoundException(string executionId)
    : Exception($"Book guidance execution '{executionId}' was not found or has expired.");

