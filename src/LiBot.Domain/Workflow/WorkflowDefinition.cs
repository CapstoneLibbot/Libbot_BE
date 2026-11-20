namespace LiBot.Domain.Workflow;
using LiBot.Domain.Enums;

public class WorkflowDefinition
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string Type { get; private set; } = "GENERAL";
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;
    public DateTime CreateAt { get; private set; } = DateTime.UtcNow;
    public bool IsActive => Status == WorkflowStatus.Active;

    public WorkflowDefinition(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void SetType(string type) => Type = string.IsNullOrWhiteSpace(type) ? "GENERAL" : type.Trim();

    public void SetActive(bool isActive) => Status = isActive ? WorkflowStatus.Active : WorkflowStatus.Inactive;

    public void SetStatus(WorkflowStatus status) => Status = status;
}
