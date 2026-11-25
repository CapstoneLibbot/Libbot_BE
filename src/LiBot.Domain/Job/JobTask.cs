using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using LiBot.Domain.Common;

namespace LiBot.Domain.Job;

public class JobTask : Entity
{
    private static readonly JsonSerializerOptions PersistenceJsonOptions = new(JsonSerializerDefaults.Web);
    private bool _persistenceStateChanged = true;

    public Guid JobId { get; private set; }
    public Guid WorkflowTaskId { get; private set; }
    public int SequenceNo { get; private set; } = 1;
    public string Name { get; private set; }
    public string Status { get; private set; }
    [NotMapped]
    public string? Description { get; private set; }
    public string RuntimeId { get; private set; } = "real";
    public string? ExecutionId { get; private set; }
    public string? ExecutionState { get; private set; }
    [NotMapped]
    public string? PersistencePayload
    {
        get => JsonSerializer.Serialize(new JobTaskPersistencePayload(Description, RuntimeId, ExecutionId, ExecutionState),
            PersistenceJsonOptions);
        private set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Description = null;
                RuntimeId = "real";
                ExecutionId = null;
                ExecutionState = null;
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(value);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object
                    && (root.TryGetProperty("runtimeId", out _) || root.TryGetProperty("description", out _)))
                {
                    var payload = JsonSerializer.Deserialize<JobTaskPersistencePayload>(value, PersistenceJsonOptions);
                    Description = payload?.Description;
                    RuntimeId = string.IsNullOrWhiteSpace(payload?.RuntimeId) ? "real" : payload.RuntimeId;
                    ExecutionId = payload?.ExecutionId;
                    ExecutionState = payload?.ExecutionState;
                    return;
                }
            }
            catch (JsonException)
            {
                // Older rows may contain plain text descriptions.
            }

            Description = value;
            RuntimeId = "real";
            ExecutionId = null;
            ExecutionState = null;
        }
    }
    [NotMapped]
    public DateTime? StartedAtUtc { get; private set; }
    [NotMapped]
    public DateTime? CompletedAtUtc { get; private set; }

    private JobTask()
    {
        Name = string.Empty;
        Status = "Pending";
    }

    public JobTask(Guid jobId, Guid workflowTaskId, int sequenceNo, string name, string runtimeId = "real", string status = "Pending")
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("Job id is required.", nameof(jobId));
        if (workflowTaskId == Guid.Empty)
            throw new ArgumentException("Workflow task id is required.", nameof(workflowTaskId));
        if (sequenceNo < 1)
            throw new ArgumentOutOfRangeException(nameof(sequenceNo));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Job task name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Job task status is required.", nameof(status));
        JobId = jobId;
        WorkflowTaskId = workflowTaskId;
        SequenceNo = sequenceNo;
        Name = name.Trim();
        Status = status.Trim();
        RuntimeId = string.IsNullOrWhiteSpace(runtimeId) ? "real" : runtimeId.Trim().ToLowerInvariant();
    }

    public void SetExecutionId(string executionId)
    {
        if (string.IsNullOrWhiteSpace(executionId))
            throw new ArgumentException("Execution id is required.", nameof(executionId));
        ExecutionId = executionId.Trim();
        _persistenceStateChanged = true;
    }

    public void PrepareForRetry()
    {
        ExecutionId = null;
        ExecutionState = null;
        StartedAtUtc = null;
        CompletedAtUtc = null;
        Status = "Pending";
        _persistenceStateChanged = true;
    }

    public void UpdateExecutionState(string executionState)
    {
        ExecutionState = executionState;
        _persistenceStateChanged = true;
        try
        {
            using var document = JsonDocument.Parse(executionState);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("status", out var statusElement)
                && statusElement.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(statusElement.GetString()))
            {
                var status = statusElement.GetString()!.Trim().ToUpperInvariant();
                Status = status switch
                {
                    "QUEUED" => "Pending",
                    "ACCEPTED" or "RUNNING" or "WAITING" => "Running",
                    "SUCCEEDED" or "COMPLETED" => "Completed",
                    "FAILED" => "Failed",
                    "CANCELLED" => "Cancelled",
                    _ => status
                };
            }
        }
        catch (JsonException)
        {
            // ExecutionState can also contain non-JSON provider messages.
        }
    }
    public void UpdateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Job task status is required.", nameof(status));
        Status = status.Trim();
    }

    public void AssignWorkflowTask(Guid workflowTaskId, string name)
    {
        if (workflowTaskId == Guid.Empty) throw new ArgumentException("Workflow task id is required.", nameof(workflowTaskId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Job task name is required.", nameof(name));
        WorkflowTaskId = workflowTaskId;
        Name = name.Trim();
    }

    public void Start(DateTime? startedAtUtc = null)
    {
        if (StartedAtUtc.HasValue)
            throw new InvalidOperationException("Task has already started.");
        StartedAtUtc = startedAtUtc ?? DateTime.UtcNow;
        Status = "Running";
        _persistenceStateChanged = true;
    }

    public void Complete(DateTime? completedAtUtc = null)
    {
        if (!StartedAtUtc.HasValue)
            throw new InvalidOperationException("Task must start before it can complete.");
        if (CompletedAtUtc.HasValue)
            throw new InvalidOperationException("Task has already completed.");
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        Status = "Completed";
        _persistenceStateChanged = true;
    }

    public bool HasUnpersistedPersistenceState => _persistenceStateChanged;

    public void MarkPersistenceStatePersisted() => _persistenceStateChanged = false;

    public string SerializePersistenceState() => JsonSerializer.Serialize(
        new JobTaskPersistenceState(Description, RuntimeId, ExecutionId, ExecutionState, StartedAtUtc, CompletedAtUtc),
        PersistenceJsonOptions);

    public void LoadPersistenceState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            PersistencePayload = null;
            StartedAtUtc = null;
            CompletedAtUtc = null;
            _persistenceStateChanged = false;
            return;
        }

        try
        {
            var state = JsonSerializer.Deserialize<JobTaskPersistenceState>(value, PersistenceJsonOptions);
            if (state is not null)
            {
                Description = state.Description;
                RuntimeId = string.IsNullOrWhiteSpace(state.RuntimeId) ? "real" : state.RuntimeId;
                ExecutionId = state.ExecutionId;
                ExecutionState = state.ExecutionState;
                StartedAtUtc = state.StartedAtUtc;
                CompletedAtUtc = state.CompletedAtUtc;
                _persistenceStateChanged = false;
                return;
            }
        }
        catch (JsonException)
        {
            // Accept rows written by the earlier persistence payload format.
        }

        PersistencePayload = value;
        StartedAtUtc = null;
        CompletedAtUtc = null;
        _persistenceStateChanged = false;
    }

    private sealed record JobTaskPersistencePayload(
        string? Description,
        string? RuntimeId,
        string? ExecutionId,
        string? ExecutionState);

    private sealed record JobTaskPersistenceState(
        string? Description,
        string? RuntimeId,
        string? ExecutionId,
        string? ExecutionState,
        DateTime? StartedAtUtc,
        DateTime? CompletedAtUtc);
}
