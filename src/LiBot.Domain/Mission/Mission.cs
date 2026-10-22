namespace LiBot.Domain.Mission;

public class Mission
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Status { get; private set; } = "Pending";
    public Guid? CreatedBy { get; private set; }
    public Guid? SourceEndpointId { get; private set; }
    public Guid? DestinationEndpointId { get; private set; }
    public Guid? WorkflowId { get; private set; }
    public int Priority { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public Mission(string name, string description, Guid? createdBy = null,
        Guid? sourceEndpointId = null, Guid? destinationEndpointId = null, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mission name is required.", nameof(name));

        Name = name.Trim();
        Description = description.Trim();
        CreatedBy = createdBy;
        SourceEndpointId = sourceEndpointId;
        DestinationEndpointId = destinationEndpointId;
        Priority = priority;
    }

    public void Start()
    {
        if (Status != "Pending")
            throw new InvalidOperationException("Only pending missions can start.");

        Status = "Running";
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Retry()
    {
        if (Status is not ("Failed" or "Cancelled"))
            throw new InvalidOperationException("Only failed or cancelled missions can be retried.");

        Status = "Pending";
        StartedAtUtc = null;
        CompletedAtUtc = null;
    }

    public void Complete()
    {
        Status = "Completed";
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string description, int priority, Guid? workflowId,
        Guid? sourceEndpointId, Guid? destinationEndpointId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mission name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Priority = priority;
        WorkflowId = workflowId;
        SourceEndpointId = sourceEndpointId;
        DestinationEndpointId = destinationEndpointId;
    }

    public void Cancel()
    {
        if (Status is "Completed" or "Cancelled")
            throw new InvalidOperationException("Completed or cancelled missions cannot be cancelled.");
        Status = "Cancelled";
        CompletedAtUtc = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status is "Completed" or "Cancelled")
            throw new InvalidOperationException("Completed or cancelled missions cannot be failed.");
        Status = "Failed";
        CompletedAtUtc = DateTime.UtcNow;
    }
}
