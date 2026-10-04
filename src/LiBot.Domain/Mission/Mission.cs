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

    public void Complete()
    {
        Status = "Completed";
        CompletedAtUtc = DateTime.UtcNow;
    }
}
