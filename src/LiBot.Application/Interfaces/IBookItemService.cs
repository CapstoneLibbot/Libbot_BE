using LiBot.Application.Models.BookItem;
namespace LiBot.Application.Interfaces;
public interface IBookItemService
{
    Task<BookItemListResponse> SearchAsync(BookItemQuery query, CancellationToken cancellationToken);
    Task<BookItemDetailResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<BookItemDetailResponse> CreateAsync(BookItemWriteRequest request, CancellationToken cancellationToken);
    Task<BookItemDetailResponse?> UpdateAsync(Guid id, BookItemWriteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
