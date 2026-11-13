using LiBot.Domain.Common;

namespace LiBot.Domain.Workflow;

public class TaskStep : Entity
{
    public Guid WorkflowTaskId { get; private set; }
    public int SequenceNo { get; private set; }
    public string StepType { get; private set; }
    public string Configuration { get; private set; }

    public TaskStep(Guid workflowTaskId, string stepType, string? configuration = null, int sequenceNo = 0)
    {
        if (workflowTaskId == Guid.Empty)
            throw new ArgumentException("Workflow task id is required.", nameof(workflowTaskId));
        if (string.IsNullOrWhiteSpace(stepType))
            throw new ArgumentException("Step type is required.", nameof(stepType));

        WorkflowTaskId = workflowTaskId;
        SequenceNo = sequenceNo;
        StepType = stepType.Trim();
        Configuration = configuration ?? string.Empty;
    }

    public void Update(string stepType, string? configuration)
    {
        if (string.IsNullOrWhiteSpace(stepType))
            throw new ArgumentException("Step type is required.", nameof(stepType));
        StepType = stepType.Trim();
        Configuration = configuration ?? string.Empty;
    }
}
