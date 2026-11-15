using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookReceipt : Entity
{
    public string ReceiptType { get; private set; }
    public DateOnly? ReceivedDate { get; private set; }
    public BookReceiptStatus Status { get; private set; }
    public int Total { get; private set; }
    public string? Note { get; private set; }

    public BookReceipt(string receiptType, BookReceiptStatus status, int total, DateOnly? receivedDate = null, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(receiptType))
            throw new ArgumentException("Receipt type is required.", nameof(receiptType));
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        ReceiptType = receiptType.Trim();
        Status = status;
        Total = total;
        ReceivedDate = receivedDate;
        Note = note?.Trim();
    }

    public BookReceipt(string receiptType, string status, int total, DateOnly? receivedDate = null, string? note = null)
        : this(receiptType, ParseStatus(status), total, receivedDate, note)
    {
    }

    public void UpdateStatus(BookReceiptStatus status) => Status = status;
    public void Update(string receiptType, DateOnly? receivedDate, string? note, int total) { if (Status != BookReceiptStatus.DRAFT) throw new InvalidOperationException("Only DRAFT receipts can be edited."); ReceiptType = receiptType.Trim(); ReceivedDate = receivedDate; Note = note?.Trim(); Total = total; SetUpdated(); }

    private static BookReceiptStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required.", nameof(status));

        if (Enum.TryParse<BookReceiptStatus>(status.Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException($"Invalid book receipt status '{status}'.", nameof(status));
    }
}
