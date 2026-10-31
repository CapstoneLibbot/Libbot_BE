using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class Book : Entity
{
    public Guid? CategoryId { get; private set; }
    public string Isbn { get; private set; }
    public string Title { get; private set; }
    public string Author { get; private set; }
    public string? Publisher { get; private set; }
    public int? PublicationYear { get; private set; }
    public string? Image { get; private set; }
    public string? Description { get; private set; }

    public Book(string isbn, string title, string author, Guid? categoryId = null,
        string? publisher = null, int? publicationYear = null, string? image = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN is required.", nameof(isbn));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));
        if (publicationYear is < 0)
            throw new ArgumentOutOfRangeException(nameof(publicationYear));

        Isbn = isbn.Trim();
        Title = title.Trim();
        Author = author.Trim();
        CategoryId = categoryId;
        Publisher = publisher?.Trim();
        PublicationYear = publicationYear;
        Image = image?.Trim();
        Description = description?.Trim();
    }
}
