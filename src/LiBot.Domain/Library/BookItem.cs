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
    public BookItemStatus Status { get; private set; } = BookItemStatus.AVAILABLE;

    public BookItem(Guid bookId, string barcode, Guid? expectedShelfLevelId = null,
        Guid? currentShelfLevelId = null, BookItemStatus status = BookItemStatus.AVAILABLE)
    {
        if (bookId == Guid.Empty)
            throw new ArgumentException("Book id is required.", nameof(bookId));
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode is required.", nameof(barcode));

        BookId = bookId;
        Barcode = barcode.Trim();
        ExpectedShelfLevelId = expectedShelfLevelId;
        CurrentShelfLevelId = currentShelfLevelId;
        Status = status;
    }

    public BookItem(Guid bookId, string barcode, Guid? expectedShelfLevelId, Guid? currentShelfLevelId, string status)
        : this(bookId, barcode, expectedShelfLevelId, currentShelfLevelId, ParseStatus(status)) { }

    public void Update(Guid bookId, string barcode, Guid? expectedShelfLevelId, Guid? currentShelfLevelId, BookItemStatus status)
    { if (bookId == Guid.Empty || string.IsNullOrWhiteSpace(barcode)) throw new ArgumentException("bookId and barcode are required."); BookId = bookId; Barcode = barcode.Trim(); ExpectedShelfLevelId = expectedShelfLevelId; CurrentShelfLevelId = currentShelfLevelId; Status = status; SetUpdated(); }

    public void RecordInventory(Guid? shelfLevelId, DateTime? recordedAtUtc = null)
    {
        CurrentShelfLevelId = shelfLevelId;
        LastInventoryAtUtc = recordedAtUtc ?? DateTime.UtcNow;
    }

    private static BookItemStatus ParseStatus(string? value) => Enum.TryParse<BookItemStatus>(value, true, out var parsed) ? parsed : throw new ArgumentException($"Invalid book item status '{value}'.", nameof(value));
}
