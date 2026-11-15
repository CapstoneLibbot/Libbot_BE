using LiBot.Application.Interfaces;
using LiBot.Application.Models.BookItem;
namespace LiBot.Application.Services;
public sealed class BookItemService(IBookItemRepository repository) : IBookItemService
{
 public async Task<BookItemListResponse> SearchAsync(BookItemQuery q, CancellationToken ct) { var page=Math.Max(1,q.Page); var size=Math.Clamp(q.PageSize,1,100); var x=await repository.SearchAsync(q with { Page=page, PageSize=size },ct); return new(x.Items,page,size,x.Total,(int)Math.Ceiling(x.Total/(double)size)); }
 public Task<BookItemDetailResponse?> GetAsync(Guid id,CancellationToken ct)=>repository.GetAsync(id,ct);
 public async Task<BookItemDetailResponse> CreateAsync(BookItemWriteRequest r,CancellationToken ct){await Validate(r,null,ct);return await repository.CreateAsync(r,ct);}
 public async Task<BookItemDetailResponse?> UpdateAsync(Guid id,BookItemWriteRequest r,CancellationToken ct){await Validate(r,id,ct);return await repository.UpdateAsync(id,r,ct);}
 public async Task<bool> DeleteAsync(Guid id,CancellationToken ct){if(await repository.HasHistoryAsync(id,ct))throw new InvalidOperationException("Cannot delete a book item with issue or inventory history.");return await repository.DeleteAsync(id,ct);}
 private async Task Validate(BookItemWriteRequest r,Guid? id,CancellationToken ct){if(r.BookId==Guid.Empty||string.IsNullOrWhiteSpace(r.Barcode))throw new ArgumentException("bookId and barcode are required.");if(!await repository.BookExistsAsync(r.BookId,ct))throw new KeyNotFoundException("Book was not found.");foreach(var level in new[]{r.ExpectedShelfLevelId,r.CurrentShelfLevelId}.Where(x=>x.HasValue).Select(x=>x!.Value))if(!await repository.ShelfLevelExistsAsync(level,ct))throw new KeyNotFoundException("Shelf level was not found.");if(await repository.BarcodeExistsAsync(r.Barcode.Trim(),id,ct))throw new InvalidOperationException("Barcode already exists.");}
}
