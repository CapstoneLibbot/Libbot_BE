using LiBot.Application.Models;
using BookCategoryEntity = LiBot.Domain.Library.BookCategory;

namespace LiBot.Application.Models.BookCategory;

public class BookCategoryDto : LibraryResourceDto<BookCategoryEntity>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public override BookCategoryEntity ToEntity() => new(Name, Description);
}
