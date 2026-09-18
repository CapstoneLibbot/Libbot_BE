namespace LiBot.Domain.Mission;

public class Mission
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Status { get; private set; } = "Pending";
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public Mission(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mission name is required.", nameof(name));

        Name = name.Trim();
        Description = description.Trim();
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
