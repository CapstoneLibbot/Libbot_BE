using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookReceipt : Entity
{
    public string ReceiptType { get; private set; }
    public DateOnly? ReceivedDate { get; private set; }
    public string Status { get; private set; }
    public int Total { get; private set; }
    public string? Note { get; private set; }

    public BookReceipt(string receiptType, string status, int total, DateOnly? receivedDate = null, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(receiptType) || string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Receipt type and status are required.");
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        ReceiptType = receiptType.Trim();
        Status = status.Trim();
        Total = total;
        ReceivedDate = receivedDate;
        Note = note?.Trim();
    }
}
