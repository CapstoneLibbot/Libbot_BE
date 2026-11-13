using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookIssue : Entity
{
    public string IssueType { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public string Status { get; private set; }
    public int Total { get; private set; }
    public string? Note { get; private set; }

    public BookIssue(string issueType, string status, int total, DateOnly? issueDate = null, string? note = null)
    {
        if (string.IsNullOrWhiteSpace(issueType) || string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Issue type and status are required.");
        if (total < 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        IssueType = issueType.Trim();
        Status = status.Trim();
        Total = total;
        IssueDate = issueDate;
        Note = note?.Trim();
    }
}
