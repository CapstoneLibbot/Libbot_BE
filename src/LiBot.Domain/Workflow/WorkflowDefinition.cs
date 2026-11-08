namespace LiBot.Domain.Workflow;
using LiBot.Domain.Enums;

public class WorkflowDefinition
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Type { get; private set; } = "GENERAL";
    public bool IsActive { get; private set; } = true;
    public WorkflowStatus Status { get; private set; } = WorkflowStatus.Draft;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public WorkflowDefinition(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));

        Name = name.Trim();
        Description = description.Trim();
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public void SetType(string type) => Type = string.IsNullOrWhiteSpace(type) ? "GENERAL" : type.Trim();

    public void SetActive(bool isActive) => IsActive = isActive;
}
