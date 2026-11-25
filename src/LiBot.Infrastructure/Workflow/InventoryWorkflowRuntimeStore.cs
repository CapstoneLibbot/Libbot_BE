using System.Collections.Concurrent;
using System.Text.Json;

namespace LiBot.Infrastructure.Workflow;

/// <summary>Transient data for Gazebo inventory runs.</summary>
public static class InventoryWorkflowRuntimeStore
{
    private static readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> Scans = new();
    private static readonly ConcurrentDictionary<Guid, string> Reports = new();
    private static readonly ConcurrentDictionary<(Guid JobId, string Key), CachedAction> Actions = new();

    public static void ResetSimulation(Guid jobId)
    {
        Scans.TryRemove(jobId, out _);
        Reports.TryRemove(jobId, out _);
        foreach (var key in Actions.Keys.Where(key => key.JobId == jobId))
            Actions.TryRemove(key, out _);
    }

    public static void AddScan(Guid jobId, object scan) =>
        Scans.GetOrAdd(jobId, _ => new ConcurrentQueue<string>()).Enqueue(JsonSerializer.Serialize(scan));

    public static IReadOnlyList<string> GetScans(Guid jobId) =>
        Scans.TryGetValue(jobId, out var scans) ? scans.ToArray() : Array.Empty<string>();

    public static void SetReport(Guid jobId, object report) =>
        Reports[jobId] = JsonSerializer.Serialize(report);

    public static T? GetReport<T>(Guid jobId)
    {
        if (!Reports.TryGetValue(jobId, out var json)) return default;
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public static bool TryGetAction(
        Guid jobId,
        string key,
        out string fingerprint,
        out string response)
    {
        if (Actions.TryGetValue((jobId, key), out var cached))
        {
            fingerprint = cached.Fingerprint;
            response = cached.Response;
            return true;
        }

        fingerprint = string.Empty;
        response = string.Empty;
        return false;
    }

    public static void AddAction(Guid jobId, string key, string fingerprint, object response) =>
        Actions[(jobId, key)] = new CachedAction(fingerprint, JsonSerializer.Serialize(response));

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record CachedAction(string Fingerprint, string Response);
}
