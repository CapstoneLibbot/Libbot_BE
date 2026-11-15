using LiBot.Application.Interfaces;
using LiBot.Application.Models.BookItem;
using LiBot.Domain.Library;
using LiBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace LiBot.Infrastructure.Repositories;
public sealed class BookItemRepository(LiBotDbContext db) : IBookItemRepository
{
 public async Task<(IReadOnlyList<BookItemDetailResponse> Items,int Total)> SearchAsync(BookItemQuery q,CancellationToken ct){var s=db.BookItems.AsNoTracking().AsQueryable();if(!string.IsNullOrWhiteSpace(q.Barcode))s=s.Where(x=>x.Barcode.Contains(q.Barcode));if(q.BookId is Guid b)s=s.Where(x=>x.BookId==b);if(q.ShelfLevelId is Guid l)s=s.Where(x=>x.ExpectedShelfLevelId==l||x.CurrentShelfLevelId==l);if(!string.IsNullOrWhiteSpace(q.Status)&&Enum.TryParse<LiBot.Domain.Enums.BookItemStatus>(q.Status,true,out var status))s=s.Where(x=>x.Status==status);var total=await s.CountAsync(ct);var items=await s.OrderBy(x=>x.Barcode).Skip((q.Page-1)*q.PageSize).Take(q.PageSize).Select(x=>ToResponse(x)).ToListAsync(ct);return(items,total);}
 public Task<BookItemDetailResponse?> GetAsync(Guid id,CancellationToken ct)=>db.BookItems.AsNoTracking().Where(x=>x.Id==id).Select(x=>ToResponse(x)).FirstOrDefaultAsync(ct);
 public Task<bool> BookExistsAsync(Guid id,CancellationToken ct)=>db.Books.AnyAsync(x=>x.Id==id,ct);
 public Task<bool> ShelfLevelExistsAsync(Guid id,CancellationToken ct)=>db.ShelfLevels.AnyAsync(x=>x.Id==id,ct);
 public Task<bool> BarcodeExistsAsync(string b,Guid? id,CancellationToken ct)=>db.BookItems.AnyAsync(x=>x.Barcode==b&&x.Id!=id,ct);
 public Task<bool> HasHistoryAsync(Guid id,CancellationToken ct)=>db.BookIssueDetails.AnyAsync(x=>x.BookItemId==id,ct);
 public async Task<BookItemDetailResponse>CreateAsync(BookItemWriteRequest r,CancellationToken ct){var e=new BookItem(r.BookId,r.Barcode,r.ExpectedShelfLevelId,r.CurrentShelfLevelId,r.Status);db.BookItems.Add(e);await db.SaveChangesAsync(ct);return(await GetAsync(e.Id,ct))!;}
 public async Task<BookItemDetailResponse?>UpdateAsync(Guid id,BookItemWriteRequest r,CancellationToken ct){var e=await db.BookItems.FindAsync([id],ct);if(e is null)return null;e.Update(r.BookId,r.Barcode,r.ExpectedShelfLevelId,r.CurrentShelfLevelId,Enum.Parse<LiBot.Domain.Enums.BookItemStatus>(r.Status,true));await db.SaveChangesAsync(ct);return await GetAsync(id,ct);}
 public async Task<bool>DeleteAsync(Guid id,CancellationToken ct){var e=await db.BookItems.FindAsync([id],ct);if(e is null)return false;db.BookItems.Remove(e);await db.SaveChangesAsync(ct);return true;}
 private static BookItemDetailResponse ToResponse(BookItem x)=>new(x.Id,x.BookId,x.Barcode,x.Status.ToString(),x.LastInventoryAtUtc,x.ExpectedShelfLevelId,x.CurrentShelfLevelId);
}
