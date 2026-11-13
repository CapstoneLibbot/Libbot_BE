namespace LiBot.Application.Models.Book;

public class BookResponse(
    Guid Id,
    Guid? CategoryId,
    string Isbn,
    string Title,
    string Author,
    string? Publisher,
    int? PublicationYear,
    string? Image,
    string? Description,
    BookCategoryResponse? Category = null,
    IReadOnlyList<BookItemResponse>? Items = null)
{
    public Guid Id { get; } = Id;
    public Guid? CategoryId { get; } = CategoryId;
    public string Isbn { get; } = Isbn;
    public string Title { get; } = Title;
    public string Author { get; } = Author;
    public string? Publisher { get; } = Publisher;
    public int? PublicationYear { get; } = PublicationYear;
    public string? Image { get; } = Image;
    public string? Description { get; } = Description;
    public BookCategoryResponse? Category { get; } = Category;
    public IReadOnlyList<BookItemResponse>? Items { get; } = Items;
}
