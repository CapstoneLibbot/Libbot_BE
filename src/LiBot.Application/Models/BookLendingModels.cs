namespace LiBot.Application.Models;

public sealed record BorrowBooksRequest(
    IReadOnlyList<Guid> BookItemIds,
    string? Note = null);

public sealed record ReturnBooksRequest(
    IReadOnlyList<Guid>? BookItemIds = null,
    string? Note = null);

public sealed record UpdateBookLoanStatusRequest(string Status);

public sealed record BookLoanItemResponse(
    Guid BookItemId,
    Guid BookId,
    string Barcode,
    string Status,
    Guid? ExpectedShelfLevelId,
    Guid? CurrentShelfLevelId);

public sealed record BookLoanResponse(
    Guid Id,
    string IssueType,
    string Status,
    DateOnly? IssueDate,
    int Total,
    string? Note,
    IReadOnlyList<BookLoanItemResponse> Items,
    Guid? ReturnReceiptId = null);

public sealed record BookReturnResponse(
    Guid Id,
    string ReceiptType,
    string Status,
    DateOnly? ReceivedDate,
    int Total,
    string? Note,
    IReadOnlyList<Guid> BookItemIds);
