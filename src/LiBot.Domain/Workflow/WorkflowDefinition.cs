namespace LiBot.Domain.Workflow;

public class WorkflowDefinition
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Type { get; private set; } = "GENERAL";
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.ACTIVE;
    public DateTime CreateAt { get; private set; } = DateTime.UtcNow;
    public bool IsActive => Status == WorkflowStatus.ACTIVE;

    public WorkflowDefinition(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    public WorkflowDefinition(string name, string? description, WorkflowStatus status)
        : this(name, description)
    {
        Status = status;
    }

    public WorkflowDefinition(string name, string? description, string status)
        : this(name, description, ParseStatus(status))
    {
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void SetType(string type) => Type = string.IsNullOrWhiteSpace(type) ? "GENERAL" : type.Trim();

    public void SetActive(bool isActive) => Status = isActive ? WorkflowStatus.ACTIVE : WorkflowStatus.INACTIVE;

    public void SetStatus(WorkflowStatus status) => Status = status;

    public void SetStatus(string status) => Status = ParseStatus(status);

    private static WorkflowStatus ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return WorkflowStatus.ACTIVE;
        if (Enum.TryParse<WorkflowStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;
        throw new ArgumentException($"Invalid workflow status '{status}'.", nameof(status));
    }
}
