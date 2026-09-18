namespace LiBot.Domain.Workflow;

public class WorkflowDefinition
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; private set; }

    public WorkflowDefinition(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow name is required.", nameof(name));

        Name = name.Trim();
        Description = description.Trim();
    }
}
