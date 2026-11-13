using LiBot.Domain.Common;

namespace LiBot.Domain.Library;

public class BookItem : Entity
{
    public Guid BookId { get; private set; }
    public string Barcode { get; private set; }
    public Guid? ExpectedShelfLevelId { get; private set; }
    public Guid? CurrentShelfLevelId { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("LastInventoryAt")]
    public DateTime? LastInventoryAtUtc { get; private set; }
    public string Status { get; private set; }

    public BookItem(Guid bookId, string barcode, Guid? expectedShelfLevelId = null,
        Guid? currentShelfLevelId = null, string status = "Available")
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("Book id is required.", nameof(bookId));
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.", nameof(barcode));
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required.", nameof(status));

        BookId = bookId;
        Barcode = barcode.Trim();
        ExpectedShelfLevelId = expectedShelfLevelId;
        CurrentShelfLevelId = currentShelfLevelId;
        Status = status.Trim();
    }

    public void RecordInventory(Guid? shelfLevelId, DateTime? recordedAtUtc = null)
    {
        CurrentShelfLevelId = shelfLevelId;
        LastInventoryAtUtc = recordedAtUtc ?? DateTime.UtcNow;
    }
}
