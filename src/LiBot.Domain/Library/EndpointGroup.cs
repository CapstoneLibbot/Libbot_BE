using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class EndpointGroup : Entity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public EndpointGroup(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Endpoint group name is required.", nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
    }
}
