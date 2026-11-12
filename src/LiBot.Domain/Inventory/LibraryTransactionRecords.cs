using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookReceipt : Entity
{
    public Guid BookId { get; private set; }
    public string ReceiptType { get; private set; }
    public DateTime? ReceivedDate { get; private set; }
    public string Status { get; private set; }
    public int Total { get; private set; }
    public string? Note { get; private set; }

    public BookReceipt(Guid bookId, string receiptType, string status, int total, DateTime? receivedDate = null, string? note = null)
    {
        if (bookId == Guid.Empty) throw new ArgumentException("Book id is required.", nameof(bookId));
        if (string.IsNullOrWhiteSpace(receiptType) || string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Receipt type and status are required.");
        if (total < 0) throw new ArgumentOutOfRangeException(nameof(total));
        BookId = bookId; ReceiptType = receiptType.Trim(); Status = status.Trim(); Total = total; ReceivedDate = receivedDate; Note = note?.Trim();
    }
}

public sealed class BookReceiptDetail : Entity
{
    public Guid ReceiptId { get; private set; }
    public Guid BookItemId { get; private set; }
    public Guid BookId { get; private set; }
    public string? Note { get; private set; }
    public BookReceiptDetail(Guid receiptId, Guid bookItemId, Guid bookId, string? note = null)
    { ReceiptId = Required(receiptId, nameof(receiptId)); BookItemId = Required(bookItemId, nameof(bookItemId)); BookId = Required(bookId, nameof(bookId)); Note = note?.Trim(); }
    private static Guid Required(Guid value, string name) => value == Guid.Empty ? throw new ArgumentException("Identifier is required.", name) : value;
}

public sealed class BookIssue : Entity
{
    public Guid BookId { get; private set; }
    public string IssueType { get; private set; }
    public DateTime? IssueDate { get; private set; }
    public string Status { get; private set; }
    public int Total { get; private set; }
    public string? Note { get; private set; }
    public BookIssue(Guid bookId, string issueType, string status, int total, DateTime? issueDate = null, string? note = null)
    { if (bookId == Guid.Empty) throw new ArgumentException("Book id is required.", nameof(bookId)); if (string.IsNullOrWhiteSpace(issueType) || string.IsNullOrWhiteSpace(status)) throw new ArgumentException("Issue type and status are required."); if (total < 0) throw new ArgumentOutOfRangeException(nameof(total)); BookId = bookId; IssueType = issueType.Trim(); Status = status.Trim(); Total = total; IssueDate = issueDate; Note = note?.Trim(); }
}

public sealed class BookIssueDetail : Entity
{
    public Guid IssueId { get; private set; }
    public Guid BookItemId { get; private set; }
    public Guid BookId { get; private set; }
    public string? Note { get; private set; }
    public BookIssueDetail(Guid issueId, Guid bookItemId, Guid bookId, string? note = null)
    { if (issueId == Guid.Empty || bookItemId == Guid.Empty || bookId == Guid.Empty) throw new ArgumentException("Identifiers are required."); IssueId = issueId; BookItemId = bookItemId; BookId = bookId; Note = note?.Trim(); }
}

public sealed class InventoryReportDetail : Entity
{
    public Guid ReportId { get; private set; }
    public Guid BookItemId { get; private set; }
    public Guid ExpectedShelfLevelId { get; private set; }
    public Guid DetectedShelfLevelId { get; private set; }
    public string Status { get; private set; }
    public string Result { get; private set; }
    public InventoryReportDetail(Guid reportId, Guid bookItemId, Guid expectedShelfLevelId, Guid detectedShelfLevelId, string status, string result)
    { if (reportId == Guid.Empty || bookItemId == Guid.Empty || expectedShelfLevelId == Guid.Empty || detectedShelfLevelId == Guid.Empty) throw new ArgumentException("Identifiers are required."); if (string.IsNullOrWhiteSpace(status) || string.IsNullOrWhiteSpace(result)) throw new ArgumentException("Status and result are required."); ReportId = reportId; BookItemId = bookItemId; ExpectedShelfLevelId = expectedShelfLevelId; DetectedShelfLevelId = detectedShelfLevelId; Status = status.Trim(); Result = result.Trim(); }
}
