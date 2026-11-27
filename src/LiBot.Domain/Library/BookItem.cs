using LiBot.Domain.Common;
using LiBot.Domain.Enums;

namespace LiBot.Domain.Library;

public class BookItem : Entity
{
    public Guid BookId { get; private set; }
    public string Barcode { get; private set; }
    public Guid? ExpectedShelfLevelId { get; private set; }
    public Guid? CurrentShelfLevelId { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("LastInventoryAt")]
    public DateTime? LastInventoryAtUtc { get; private set; }
    public BookItemStatus Status { get; private set; }

    public BookItem(Guid bookId, string barcode, Guid? expectedShelfLevelId = null,
        Guid? currentShelfLevelId = null, BookItemStatus status = BookItemStatus.Available)
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

    public void RecordInventory(Guid? shelfLevelId, DateTime? recordedAtUtc = null)
    {
        CurrentShelfLevelId = shelfLevelId;
        LastInventoryAtUtc = recordedAtUtc ?? DateTime.UtcNow;
    }

    public void ApplyInventoryResult(Guid? shelfLevelId, string? status, DateTime? recordedAtUtc = null)
    {
        if (status is not null && string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Inventory status cannot be empty.", nameof(status));

        CurrentShelfLevelId = shelfLevelId;
        LastInventoryAtUtc = recordedAtUtc ?? DateTime.UtcNow;
        if (status is not null)
            Status = status.ToLowerInvariant() switch
            {
                "available" => BookItemStatus.Available,
                "misplaced" => BookItemStatus.Misplaced,
                "missing" => BookItemStatus.Missing,
                "damaged" => BookItemStatus.Damaged,
                "borrowed" => BookItemStatus.Borrowed,
                _ => throw new ArgumentException($"Invalid inventory status '{status}'.", nameof(status))
            };
    }

    public void Borrow()
    {
        if (Status != BookItemStatus.Available)
            throw new InvalidOperationException($"Book item '{Barcode}' is not available for borrowing.");
        Status = BookItemStatus.Borrowed;
        SetUpdated();
    }

    public void ReturnToExpectedShelf()
    {
        CurrentShelfLevelId = ExpectedShelfLevelId;
        LastInventoryAtUtc = DateTime.UtcNow;
        Status = BookItemStatus.Available;
        SetUpdated();
    }
}
