using System.Collections.Concurrent;
using System.Text.Json;

namespace LiBot.Infrastructure.Workflow;

/// <summary>Transient data for Gazebo inventory runs.</summary>
public static class InventoryWorkflowRuntimeStore
{
    private static readonly ConcurrentDictionary<Guid, ConcurrentQueue<string>> Scans = new();
    private static readonly ConcurrentDictionary<Guid, string> Reports = new();
    private static readonly ConcurrentDictionary<(Guid MissionId, string Key), CachedAction> Actions = new();

    public static void ResetSimulation(Guid missionId)
    {
        Scans.TryRemove(missionId, out _);
        Reports.TryRemove(missionId, out _);
        foreach (var key in Actions.Keys.Where(key => key.MissionId == missionId))
            Actions.TryRemove(key, out _);
    }

    public static void AddScan(Guid missionId, object scan) =>
        Scans.GetOrAdd(missionId, _ => new ConcurrentQueue<string>()).Enqueue(JsonSerializer.Serialize(scan));

    public static IReadOnlyList<string> GetScans(Guid missionId) =>
        Scans.TryGetValue(missionId, out var scans) ? scans.ToArray() : Array.Empty<string>();

    public static void SetReport(Guid missionId, object report) =>
        Reports[missionId] = JsonSerializer.Serialize(report);

    public static T? GetReport<T>(Guid missionId)
    {
        if (!Reports.TryGetValue(missionId, out var json)) return default;
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public static bool TryGetAction(
        Guid missionId,
        string key,
        out string fingerprint,
        out string response)
    {
        if (Actions.TryGetValue((missionId, key), out var cached))
        {
            fingerprint = cached.Fingerprint;
            response = cached.Response;
            return true;
        }

        fingerprint = string.Empty;
        response = string.Empty;
        return false;
    }

    public static void AddAction(Guid missionId, string key, string fingerprint, object response) =>
        Actions[(missionId, key)] = new CachedAction(fingerprint, JsonSerializer.Serialize(response));

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed record CachedAction(string Fingerprint, string Response);
}
