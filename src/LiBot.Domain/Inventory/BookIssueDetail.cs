using LiBot.Domain.Common;

namespace LiBot.Domain.Inventory;

public sealed class BookIssueDetail : Entity
{
    public Guid IssueId { get; private set; }
    public Guid BookItemId { get; private set; }
    public string? Note { get; private set; }

    public BookIssueDetail(Guid issueId, Guid bookItemId, string? note = null)
    {
        if (issueId == Guid.Empty || bookItemId == Guid.Empty)
            throw new ArgumentException("Identifiers are required.");

        IssueId = issueId;
        BookItemId = bookItemId;
        Note = note?.Trim();
    }
}
