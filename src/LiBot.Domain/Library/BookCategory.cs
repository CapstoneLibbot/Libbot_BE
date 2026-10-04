using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class BookCategory : Entity
{
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public BookCategory(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
    }
}