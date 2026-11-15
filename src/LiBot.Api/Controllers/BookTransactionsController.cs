using LiBot.Application.Interfaces; using LiBot.Application.Models; using LiBot.Application.Models.BookTransaction; using Microsoft.AspNetCore.Mvc;
namespace LiBot.Api.Controllers;
[ApiController]
public sealed class BookTransactionsController(IBookTransactionService service):ControllerBase
{
 [HttpGet("api/book-receipts")] public async Task<IActionResult>Receipts(int page=1,int pageSize=20,string? receiptType=null,string? status=null,string? search=null,DateOnly? fromDate=null,DateOnly? toDate=null,CancellationToken ct=default)=>Ok(new BaseResponse<TransactionListResponse>(await service.ListReceiptsAsync(page,pageSize,receiptType,status,search,fromDate,toDate,ct)));
 [HttpGet("api/book-receipts/{id:guid}")] public async Task<IActionResult>Receipt(Guid id,CancellationToken ct){var x=await service.GetReceiptAsync(id,ct);return x==null?NotFound(BaseResponse<object>.Failure("Receipt not found.")):Ok(new BaseResponse<TransactionResponse>(x));}
 [HttpPost("api/book-receipts")] public async Task<IActionResult>CreateReceipt(CreateBookReceiptRequest r,CancellationToken ct)=>await Run(()=>service.CreateReceiptAsync(r,ct),nameof(Receipt),ct);
 [HttpPut("api/book-receipts/{id:guid}")] public async Task<IActionResult>UpdateReceipt(Guid id,CreateBookReceiptRequest r,CancellationToken ct)=>await Run(()=>service.UpdateReceiptAsync(id,r,ct),nameof(Receipt),ct);
 [HttpPut("api/book-receipts/{id:guid}/status")] public async Task<IActionResult>ReceiptStatus(Guid id,UpdateTransactionStatusRequest r,CancellationToken ct)=>await Run(()=>service.UpdateReceiptStatusAsync(id,r.Status,ct),nameof(Receipt),ct);
 [HttpGet("api/book-issues")] public async Task<IActionResult>Issues(int page=1,int pageSize=20,string? issueType=null,string? status=null,string? search=null,DateOnly? fromDate=null,DateOnly? toDate=null,CancellationToken ct=default)=>Ok(new BaseResponse<TransactionListResponse>(await service.ListIssuesAsync(page,pageSize,issueType,status,search,fromDate,toDate,ct)));
 [HttpGet("api/book-issues/{id:guid}")] public async Task<IActionResult>Issue(Guid id,CancellationToken ct){var x=await service.GetIssueAsync(id,ct);return x==null?NotFound(BaseResponse<object>.Failure("Issue not found.")):Ok(new BaseResponse<TransactionResponse>(x));}
 [HttpPost("api/book-issues")] public async Task<IActionResult>CreateIssue(CreateBookIssueRequest r,CancellationToken ct)=>await Run(()=>service.CreateIssueAsync(r,ct),nameof(Issue),ct);
 [HttpPut("api/book-issues/{id:guid}")] public async Task<IActionResult>UpdateIssue(Guid id,CreateBookIssueRequest r,CancellationToken ct)=>await Run(()=>service.UpdateIssueAsync(id,r,ct),nameof(Issue),ct);
 [HttpPut("api/book-issues/{id:guid}/status")] public async Task<IActionResult>IssueStatus(Guid id,UpdateTransactionStatusRequest r,CancellationToken ct)=>await Run(()=>service.UpdateIssueStatusAsync(id,r.Status,ct),nameof(Issue),ct);
 async Task<IActionResult>Run(Func<Task<TransactionResponse>> work,string action,CancellationToken ct){try{var x=await work();return CreatedAtAction(action,new{id=x.Id},new BaseResponse<TransactionResponse>(x));}catch(KeyNotFoundException e){return NotFound(BaseResponse<object>.Failure(e.Message));}catch(Exception e)when(e is ArgumentException or InvalidOperationException){return BadRequest(BaseResponse<object>.Failure(e.Message));}}
}
