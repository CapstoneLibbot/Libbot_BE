using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookIssue : Entity
{
    public string IssueType { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public BookIssueStatus Status { get; private set; }
    public int Total { get; private set; }
    public string? Note { get; private set; }

    public BookIssue(string issueType, BookIssueStatus status, int total, DateOnly? issueDate = null, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(issueType)) throw new ArgumentException("Issue type is required.");
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        IssueType = issueType.Trim();
        Status = status;
        Total = total;
        IssueDate = issueDate;
        Note = note?.Trim();
    }
    public BookIssue(string issueType, string status, int total, DateOnly? issueDate = null, string? note = null) : this(issueType, Enum.Parse<BookIssueStatus>(status, true), total, issueDate, note) { }
    public void UpdateStatus(BookIssueStatus status) { Status = status; SetUpdated(); }
    public void Update(string issueType, DateOnly? issueDate, string? note, int total) { if (Status != BookIssueStatus.DRAFT) throw new InvalidOperationException("Only DRAFT issues can be edited."); IssueType = issueType.Trim(); IssueDate = issueDate; Note = note?.Trim(); Total = total; SetUpdated(); }
}
