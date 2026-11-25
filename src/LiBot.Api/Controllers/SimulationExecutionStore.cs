using System.Collections.Concurrent;
using System.Text.Json;

namespace LiBot.Api.Controllers;

/// <summary>
/// Process-local execution state for Gazebo runs. SIM step events stay in memory;
/// direct guidance runs may persist their final job summary without creating JobEvents.
/// </summary>
internal static class SimulationExecutionStore
{
    private static readonly ConcurrentDictionary<Guid, Entry> Entries = new();

    public static void TryStoreInitialContext(Guid jobId, string? initialContext)
    {
        if (jobId == Guid.Empty) return;
        var entry = Entries.GetOrAdd(jobId, _ => new Entry());
        lock (entry.Sync) entry.InitialContext = initialContext;
    }

    public static bool TryGetInitialContext(Guid jobId, out string? initialContext)
    {
        if (Entries.TryGetValue(jobId, out var entry))
        {
            lock (entry.Sync)
            {
                initialContext = entry.InitialContext;
                return true;
            }
        }

        initialContext = null;
        return false;
    }

    public static bool TryStart(Guid jobId, string executionId, string runtimeId, object state,
        bool persistJobStatus = false)
    {
        var entry = Entries.GetOrAdd(jobId, _ => new Entry());
        lock (entry.Sync)
        {
            if (IsActive(entry.Status)) return false;
            entry.ExecutionId = executionId;
            entry.RuntimeId = runtimeId;
            entry.Status = "ACCEPTED";
            entry.CurrentState = Serialize(state);
            entry.PersistJobStatus = persistJobStatus;
            entry.Events.Clear();
            SimulationWorkflowDataStore.Reset(jobId);
            return true;
        }
    }

    public static bool TryGet(Guid jobId, out Snapshot snapshot)
    {
        if (!Entries.TryGetValue(jobId, out var entry))
        {
            snapshot = default!;
            return false;
        }

        lock (entry.Sync)
        {
            snapshot = new Snapshot(
                entry.ExecutionId,
                entry.RuntimeId,
                Parse(entry.CurrentState),
                entry.Events.Select(Parse).ToArray(),
                entry.Status,
                entry.PersistJobStatus);
            return true;
        }
    }

    public static bool TryUpdate(Guid jobId, string? executionId, object state, bool recordEvent = false)
    {
        if (!Entries.TryGetValue(jobId, out var entry)) return false;
        lock (entry.Sync)
        {
            if (!MatchesExecution(entry, executionId) || IsTerminal(entry.Status)) return false;
            var json = Serialize(state);
            var status = ReadStatus(json);
            if (string.IsNullOrWhiteSpace(status)) return false;
            entry.Status = status;
            entry.CurrentState = json;
            if (recordEvent) entry.Events.Add(json);
            return true;
        }
    }

    public static bool TryGetExecution(Guid jobId, out string executionId, out string runtimeId)
    {
        if (Entries.TryGetValue(jobId, out var entry))
        {
            lock (entry.Sync)
            {
                executionId = entry.ExecutionId;
                runtimeId = entry.RuntimeId;
                return !string.IsNullOrWhiteSpace(executionId);
            }
        }

        executionId = string.Empty;
        runtimeId = string.Empty;
        return false;
    }

    private static bool MatchesExecution(Entry entry, string? executionId) =>
        string.IsNullOrWhiteSpace(executionId)
            ? !string.IsNullOrWhiteSpace(entry.ExecutionId)
            : string.Equals(entry.ExecutionId, executionId, StringComparison.Ordinal);

    private static bool IsActive(string? status) =>
        status is "ACCEPTED" or "RUNNING" or "WAITING" or "STEP_COMPLETED" or "STEP_FAILED";

    private static bool IsTerminal(string? status) =>
        status is "SUCCEEDED" or "COMPLETED" or "FAILED" or "CANCELLED";

    private static string Serialize(object state) => JsonSerializer.Serialize(state);

    private static JsonElement Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static string? ReadStatus(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("status", out var status)
            ? status.GetString()?.ToUpperInvariant()
            : null;
    }

    private sealed class Entry
    {
        public object Sync { get; } = new();
        public string ExecutionId { get; set; } = string.Empty;
        public string RuntimeId { get; set; } = "sim";
        public string Status { get; set; } = "QUEUED";
        public string CurrentState { get; set; } = "{}";
        public string? InitialContext { get; set; }
        public bool PersistJobStatus { get; set; }
        public List<string> Events { get; } = [];
    }

    internal sealed record Snapshot(
        string ExecutionId,
        string RuntimeId,
        JsonElement CurrentState,
        IReadOnlyList<JsonElement> Events,
        string Status,
        bool PersistJobStatus);
}
