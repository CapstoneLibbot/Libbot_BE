using LiBot.Application.Models;
using BookItemEntity = LiBot.Domain.Library.BookItem;

namespace LiBot.Application.Models.BookItem;

public class BookItemDto : LibraryResourceDto<BookItemEntity>
{
    public Guid BookId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public Guid? ExpectedShelfLevelId { get; set; }
    public Guid? CurrentShelfLevelId { get; set; }
    public DateTime? LastInventoryAtUtc { get; set; }
    public string Status { get; set; } = "Available";

    public override BookItemEntity ToEntity() => new(BookId, Barcode, ExpectedShelfLevelId, CurrentShelfLevelId, Status);
}
