using LiBot.Domain.Common;

namespace LiBot.Domain.Workflow;

public class WorkflowTask : Entity
{
    public Guid WorkflowId { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("SequenceNo")]
    public int SequenceNumber { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public WorkflowTask(Guid workflowId, int sequenceNumber, string name, string? description = null)
    {
        if (workflowId == Guid.Empty)
            throw new ArgumentException("Workflow id is required.", nameof(workflowId));
        if (sequenceNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow task name is required.", nameof(name));

        WorkflowId = workflowId;
        SequenceNumber = sequenceNumber;
        Name = name.Trim();
        Description = description?.Trim();
    }

    public void Update(int sequenceNumber, string name, string? description = null)
    {
        if (sequenceNumber < 0)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workflow task name is required.", nameof(name));
        SequenceNumber = sequenceNumber;
        Name = name.Trim();
        Description = description?.Trim();
    }
}
