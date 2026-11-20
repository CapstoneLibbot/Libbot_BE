using LiBot.Application.Models;
using LiBot.Domain.Enums;
using LiBot.Domain.Inventory;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiBot.Api.Controllers;

[ApiController]
[Route("api/book-loans")]
public sealed class BookLendingController(LiBotDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookLoanResponse>>> GetLoans(
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = db.BookIssues.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(item => item.Status == status.Trim());

        var issues = await query.OrderByDescending(item => item.IssueDate).ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        return Ok(await BuildLoanResponsesAsync(issues, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookLoanResponse>> GetLoan(Guid id, CancellationToken cancellationToken)
    {
        var issue = await db.BookIssues.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (issue is null) return NotFound(new { message = "Book loan was not found." });
        return Ok((await BuildLoanResponsesAsync([issue], cancellationToken)).Single());
    }

    [HttpPost]
    public async Task<ActionResult<BookLoanResponse>> Borrow(
        [FromBody] BorrowBooksRequest request,
        CancellationToken cancellationToken)
    {
        var ids = NormalizeIds(request.BookItemIds);
        if (ids.Count == 0) return BadRequest(new { message = "At least one book item is required." });

        var items = await db.BookItems.Where(item => ids.Contains(item.Id)).ToListAsync(cancellationToken);
        if (items.Count != ids.Count)
            return NotFound(new { message = "One or more book items were not found." });
        if (items.Any(item => item.Status != BookItemStatus.Available))
            return Conflict(new { message = "Only available book items can be borrowed." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var issue = new BookIssue("BORROW", "Borrowed", items.Count,
            DateOnly.FromDateTime(DateTime.UtcNow), request.Note);
        db.BookIssues.Add(issue);
        foreach (var item in items)
        {
            item.Borrow();
            db.BookIssueDetails.Add(new BookIssueDetail(issue.Id, item.Id, item.BookId));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(GetLoan), new { id = issue.Id },
            (await BuildLoanResponsesAsync([issue], cancellationToken)).Single());
    }

    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<BookLoanResponse>> Return(
        Guid id,
        [FromBody] ReturnBooksRequest? request,
        CancellationToken cancellationToken)
    {
        var issue = await db.BookIssues.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (issue is null) return NotFound(new { message = "Book loan was not found." });
        if (issue.Status is "Returned" or "Cancelled")
            return Conflict(new { message = $"Book loan is already {issue.Status.ToLowerInvariant()}." });

        var details = await db.BookIssueDetails.Where(item => item.IssueId == id).ToListAsync(cancellationToken);
        var selectedIds = request?.BookItemIds is { Count: > 0 }
            ? NormalizeIds(request.BookItemIds)
            : details.Select(item => item.BookItemId).ToHashSet();
        if (selectedIds.Count == 0) return BadRequest(new { message = "At least one book item is required." });
        if (selectedIds.Any(itemId => details.All(detail => detail.BookItemId != itemId)))
            return BadRequest(new { message = "One or more book items do not belong to this loan." });

        var items = await db.BookItems.Where(item => selectedIds.Contains(item.Id)).ToListAsync(cancellationToken);
        if (items.Any(item => item.Status != BookItemStatus.Borrowed))
            return Conflict(new { message = "One or more selected book items are not currently borrowed." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var receipt = new BookReceipt("RETURN", "Completed", items.Count,
            DateOnly.FromDateTime(DateTime.UtcNow), request?.Note);
        db.BookReceipts.Add(receipt);
        foreach (var item in items)
        {
            item.ReturnToExpectedShelf();
            db.BookReceiptDetails.Add(new BookReceiptDetail(receipt.Id, item.Id, item.BookId));
        }

        var allReturned = details.All(detail => !selectedIds.Contains(detail.BookItemId)
            ? db.BookItems.Any(item => item.Id == detail.BookItemId && item.Status != BookItemStatus.Borrowed)
            : true);
        issue.UpdateStatus(allReturned ? "Returned" : "PartiallyReturned");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok((await BuildLoanResponsesAsync([issue], cancellationToken)).Single() with
        {
            ReturnReceiptId = receipt.Id
        });
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<BookLoanResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateBookLoanStatusRequest request,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeLoanStatus(request.Status);
        if (normalized is null)
            return BadRequest(new { message = "status must be Pending, Borrowed, PartiallyReturned, Returned, Overdue or Cancelled." });

        var issue = await db.BookIssues.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (issue is null) return NotFound(new { message = "Book loan was not found." });
        if (normalized is "Returned" or "Cancelled")
            return BadRequest(new { message = "Use the return or cancel workflow endpoint to change a loan to this status." });

        issue.UpdateStatus(normalized);
        await db.SaveChangesAsync(cancellationToken);
        return Ok((await BuildLoanResponsesAsync([issue], cancellationToken)).Single());
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BookLoanResponse>> Cancel(
        Guid id, CancellationToken cancellationToken)
    {
        var issue = await db.BookIssues.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (issue is null) return NotFound(new { message = "Book loan was not found." });
        if (issue.Status is "Returned" or "Cancelled")
            return Conflict(new { message = $"Book loan is already {issue.Status.ToLowerInvariant()}." });

        var details = await db.BookIssueDetails.Where(item => item.IssueId == id).ToListAsync(cancellationToken);
        var itemIds = details.Select(item => item.BookItemId).ToHashSet();
        var items = await db.BookItems.Where(item => itemIds.Contains(item.Id)).ToListAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach (var item in items.Where(item => item.Status == BookItemStatus.Borrowed))
            item.ReturnToExpectedShelf();
        issue.UpdateStatus("Cancelled");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok((await BuildLoanResponsesAsync([issue], cancellationToken)).Single());
    }

    [HttpGet("returns")]
    public async Task<ActionResult<IReadOnlyList<BookReturnResponse>>> GetReturns(CancellationToken cancellationToken)
    {
        var receipts = await db.BookReceipts.AsNoTracking()
            .Where(item => item.ReceiptType == "RETURN")
            .OrderByDescending(item => item.ReceivedDate).ThenByDescending(item => item.Id)
            .ToListAsync(cancellationToken);
        var receiptIds = receipts.Select(item => item.Id).ToArray();
        var details = await db.BookReceiptDetails.AsNoTracking()
            .Where(item => receiptIds.Contains(item.ReceiptId)).ToListAsync(cancellationToken);
        return Ok(receipts.Select(receipt => new BookReturnResponse(
            receipt.Id, receipt.ReceiptType, receipt.Status, receipt.ReceivedDate, receipt.Total, receipt.Note,
            details.Where(item => item.ReceiptId == receipt.Id).Select(item => item.BookItemId).ToList())).ToList());
    }

    private async Task<IReadOnlyList<BookLoanResponse>> BuildLoanResponsesAsync(
        IReadOnlyList<BookIssue> issues,
        CancellationToken cancellationToken)
    {
        var issueIds = issues.Select(item => item.Id).ToArray();
        var details = await db.BookIssueDetails.AsNoTracking()
            .Where(item => issueIds.Contains(item.IssueId)).ToListAsync(cancellationToken);
        var itemIds = details.Select(item => item.BookItemId).ToArray();
        var items = await db.BookItems.AsNoTracking()
            .Where(item => itemIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        return issues.Select(issue => new BookLoanResponse(
            issue.Id,
            issue.IssueType,
            issue.Status,
            issue.IssueDate,
            issue.Total,
            issue.Note,
            details.Where(detail => detail.IssueId == issue.Id)
                .Where(detail => items.ContainsKey(detail.BookItemId))
                .Select(detail =>
                {
                    var item = items[detail.BookItemId];
                    return new BookLoanItemResponse(item.Id, item.BookId, item.Barcode, item.Status.ToString(),
                        item.ExpectedShelfLevelId, item.CurrentShelfLevelId);
                }).ToList())).ToList();
    }

    private static HashSet<Guid> NormalizeIds(IEnumerable<Guid> ids) =>
        ids.Where(id => id != Guid.Empty).ToHashSet();

    private static string? NormalizeLoanStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        var value = status.Trim();
        return new[] { "Pending", "Borrowed", "PartiallyReturned", "Returned", "Overdue", "Cancelled" }
            .FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
    }
}
