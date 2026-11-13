namespace LiBot.Domain.Mission;

public class Mission
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; } = "Pending";
    public Guid? CreatedBy { get; private set; }
    public Guid RobotId { get; private set; }
    public Guid WorkflowId { get; private set; }
    public int Priority { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("CreateAt")]
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private Mission() => Name = string.Empty;

    public Mission(string name, string? description, Guid robotId, Guid workflowId, Guid? createdBy = null, int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mission name is required.", nameof(name));
        if (robotId == Guid.Empty)
            throw new ArgumentException("Robot id is required.", nameof(robotId));
        if (workflowId == Guid.Empty)
            throw new ArgumentException("Workflow id is required.", nameof(workflowId));

        Name = name.Trim();
        Description = description?.Trim();
        CreatedBy = createdBy;
        RobotId = robotId;
        WorkflowId = workflowId;
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

    public void Update(string name, string? description, int priority, Guid workflowId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mission name is required.", nameof(name));
        if (workflowId == Guid.Empty)
            throw new ArgumentException("Workflow id is required.", nameof(workflowId));
        Name = name.Trim();
        Description = description?.Trim();
        Priority = priority;
        WorkflowId = workflowId;
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
