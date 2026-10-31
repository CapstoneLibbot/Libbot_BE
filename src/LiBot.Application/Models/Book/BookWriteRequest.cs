namespace LiBot.Application.Models.Book;

public class BookWriteRequest(
    Guid? CategoryId,
    string Isbn,
    string Title,
    string Author,
    string? Publisher = null,
    int? PublicationYear = null,
    string? Image = null)
{
    public Guid? CategoryId { get; } = CategoryId;
    public string Isbn { get; } = Isbn;
    public string Title { get; } = Title;
    public string Author { get; } = Author;
    public string? Publisher { get; } = Publisher;
    public int? PublicationYear { get; } = PublicationYear;
    public string? Image { get; } = Image;
}
