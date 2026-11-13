namespace LiBot.Application.Models.Book;

public class BookResponse
{
    public BookResponse(Guid id, Guid? categoryId, string isbn, string title, string author, string? publisher,
        int? publicationYear, string? image, string? description, BookCategoryResponse? category = null,
        IReadOnlyList<BookItemResponse>? items = null)
        : this(id, categoryId, isbn, title, author, publisher, publicationYear, image, description,
            default, null, category, items) { }

    public BookResponse(Guid id, Guid? categoryId, string isbn, string title, string author, string? publisher,
        int? publicationYear, string? image, string? description, DateTime createdAtUtc, DateTime? updatedAtUtc,
        BookCategoryResponse? category = null, IReadOnlyList<BookItemResponse>? items = null)
    {
        Id = id; CategoryId = categoryId; Isbn = isbn; Title = title; Author = author; Publisher = publisher;
        PublicationYear = publicationYear; Image = image; Description = description;
        CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc; Category = category; Items = items;
    }

    public Guid Id { get; }
    public Guid? CategoryId { get; }
    public string Isbn { get; }
    public string Title { get; }
    public string Author { get; }
    public string? Publisher { get; }
    public int? PublicationYear { get; }
    public string? Image { get; }
    public string? Description { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? UpdatedAtUtc { get; }
    public BookCategoryResponse? Category { get; }
    public IReadOnlyList<BookItemResponse>? Items { get; }
}
