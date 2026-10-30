namespace LiBot.Application.Models.Book;

public class BookCategoryResponse(Guid Id, string Name, string? Description)
{
    public Guid Id { get; } = Id;
    public string Name { get; } = Name;
    public string? Description { get; } = Description;
}
