using Microsoft.AspNetCore.Mvc;

namespace LiBot.Api.Controllers;

/// <summary>Các thao tác CRUD dùng chung cho dữ liệu thư viện.</summary>
/// <remarks>Các controller kế thừa cung cấp danh sách, chi tiết, tạo, cập nhật và xóa từng loại tài nguyên.</remarks>
public abstract class LibraryResourceController<TEntity, TDto> : ControllerBase
    where TEntity : class
    where TDto : class, ILibraryResourceDto<TEntity>, new()
{
    protected ILibraryResourceService<TEntity, TDto> ResourceService => service;

    /// <summary>Lấy toàn bộ tài nguyên của nhóm hiện tại.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await _service.GetAllAsync(cancellationToken));

    /// <summary>Lấy chi tiết tài nguyên theo ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetByIdAsync(id, cancellationToken);
        return entity is null ? NotFound() : Ok(entity);
    }

    /// <summary>Tạo tài nguyên mới từ JSON body.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> Create([FromBody] TDto dto, CancellationToken cancellationToken)
    public virtual async Task<IActionResult> Create([FromBody] TDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// <summary>Thay thông tin tài nguyên theo ID.</summary>
    [HttpPut("{id:guid}")]
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] TDto dto, CancellationToken cancellationToken)
    public virtual async Task<IActionResult> Update(Guid id, [FromBody] TDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    /// <summary>Xóa tài nguyên theo ID.</summary>
    [HttpDelete("{id:guid}")]
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    public virtual async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        return await _service.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
    }
}
