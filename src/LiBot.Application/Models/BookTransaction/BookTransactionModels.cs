namespace LiBot.Application.Models.BookTransaction;
public sealed record TransactionItem(Guid? BookItemId, Guid BookId, string Barcode, Guid? ExpectedShelfLevelId, string? Note);
public sealed record CreateBookReceiptRequest(string ReceiptType, DateOnly ReceivedDate, string? Status, string? Note, IReadOnlyList<TransactionItem> Items);
public sealed record CreateBookIssueRequest(string IssueType, DateOnly IssueDate, string? Status, string? Note, IReadOnlyList<TransactionItem> Items);
public sealed record UpdateTransactionStatusRequest(string Status);
public sealed record TransactionDetail(Guid Id, Guid BookItemId, Guid BookId, string Barcode, string Title, string Isbn, string? ShelfLevelName, string? Note)
{
    public string? ExpectedShelfName => ShelfLevelName;
    public string? CurrentShelfName => ShelfLevelName;
}
public sealed record TransactionResponse(Guid Id, string Type, DateOnly? Date, string Status, int Total, string? Note, IReadOnlyList<TransactionDetail> Details)
{
    public string ReceiptType => Type;
    public string IssueType => Type;
    public DateOnly? ReceivedDate => Date;
    public DateOnly? IssueDate => Date;
}
public sealed record TransactionListResponse(IReadOnlyList<TransactionResponse> Items, int Page, int PageSize, int TotalCount, int TotalPages);
