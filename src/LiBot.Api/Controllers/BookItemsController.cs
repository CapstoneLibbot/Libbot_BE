using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Models.BookItem;
using Microsoft.AspNetCore.Mvc;
namespace LiBot.Api.Controllers;
[ApiController][Route("api/book-items")]
public sealed class BookItemsController(IBookItemService service):ControllerBase
{
 [HttpGet] public async Task<IActionResult> Search([FromQuery]int page=1,[FromQuery]int pageSize=20,[FromQuery]string? barcode=null,[FromQuery]Guid? bookId=null,[FromQuery]string? status=null,[FromQuery]Guid? shelfLevelId=null,CancellationToken ct=default)=>Ok(new BaseResponse<BookItemListResponse>(await service.SearchAsync(new(page,pageSize,barcode,bookId,status,shelfLevelId),ct)));
 [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id,CancellationToken ct){var x=await service.GetAsync(id,ct);return x is null?NotFound(BaseResponse<object>.Failure("Book item was not found.")):Ok(new BaseResponse<BookItemDetailResponse>(x));}
 [HttpPost] public async Task<IActionResult>Create(BookItemWriteRequest r,CancellationToken ct){try{var x=await service.CreateAsync(r,ct);return CreatedAtAction(nameof(Get),new{id=x.Id},new BaseResponse<BookItemDetailResponse>(x));}catch(KeyNotFoundException e){return NotFound(BaseResponse<object>.Failure(e.Message));}catch(Exception e)when(e is ArgumentException or InvalidOperationException){return Conflict(BaseResponse<object>.Failure(e.Message));}}
 [HttpPut("{id:guid}")] public async Task<IActionResult>Update(Guid id,BookItemWriteRequest r,CancellationToken ct){try{var x=await service.UpdateAsync(id,r,ct);return x is null?NotFound(BaseResponse<object>.Failure("Book item was not found.")):Ok(new BaseResponse<BookItemDetailResponse>(x));}catch(KeyNotFoundException e){return NotFound(BaseResponse<object>.Failure(e.Message));}catch(Exception e)when(e is ArgumentException or InvalidOperationException){return Conflict(BaseResponse<object>.Failure(e.Message));}}
 [HttpDelete("{id:guid}")] public async Task<IActionResult>Delete(Guid id,CancellationToken ct){try{return await service.DeleteAsync(id,ct)?Ok(BaseResponse<object>.SuccessMessage("Book item deleted successfully.")):NotFound(BaseResponse<object>.Failure("Book item was not found."));}catch(InvalidOperationException e){return Conflict(BaseResponse<object>.Failure(e.Message));}}
}
