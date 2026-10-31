using System.Reflection;
using LiBot.Domain.Common;
using AisleEntity = LiBot.Domain.Library.Aisle;
using AreaEntity = LiBot.Domain.Library.Area;
using BookEntity = LiBot.Domain.Library.Book;
using BookCategoryEntity = LiBot.Domain.Library.BookCategory;
using BookItemEntity = LiBot.Domain.Library.BookItem;
using EndpointEntity = LiBot.Domain.Library.Endpoint;
using EndpointGroupEntity = LiBot.Domain.Library.EndpointGroup;
using EndpointGroupMemberEntity = LiBot.Domain.Library.EndpointGroupMember;
using FloorEntity = LiBot.Domain.Library.Floor;
using ShelfEntity = LiBot.Domain.Library.Shelf;
using ShelfLevelEntity = LiBot.Domain.Library.ShelfLevel;
using ZoneEntity = LiBot.Domain.Library.Zone;

namespace LiBot.Application.Models;

public interface ILibraryResourceDto<TEntity> where TEntity : class
{
    Guid Id { get; }
    TEntity ToEntity();
    void LoadFrom(TEntity entity);
    void ApplyTo(TEntity entity);
}

public abstract class LibraryResourceDto<TEntity> : ILibraryResourceDto<TEntity> where TEntity : Entity
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public abstract TEntity ToEntity();

    public void LoadFrom(TEntity entity)
    {
        foreach (var source in typeof(TEntity).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            GetType().GetProperty(source.Name)?.SetValue(this, source.GetValue(entity));
    }

    public virtual void ApplyTo(TEntity entity)
    {
        foreach (var source in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (source.Name is nameof(Id) or nameof(CreatedAtUtc) or nameof(UpdatedAtUtc)) continue;
            var target = typeof(TEntity).GetProperty(source.Name);
            if (target?.GetSetMethod(true) is not null) target.SetValue(entity, source.GetValue(this));
        }
    }
}

public sealed class AisleDto : LibraryResourceDto<AisleEntity>
{
    public Guid FloorId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override AisleEntity ToEntity() => new(FloorId, Code, Name, Description);
}

public sealed class AreaDto : LibraryResourceDto<AreaEntity>
{
    public Guid FloorId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override AreaEntity ToEntity() => new(FloorId, Name, Description, EndpointId);
}

public sealed class BookDto : LibraryResourceDto<BookEntity>
{
    public Guid? CategoryId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Image { get; set; }
    public string? Description { get; set; }
    public override BookEntity ToEntity() => new(Isbn, Title, Author, CategoryId, Publisher, PublicationYear, Image, Description);
}

public sealed class BookCategoryDto : LibraryResourceDto<BookCategoryEntity>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override BookCategoryEntity ToEntity() => new(Name, Description);
}

public sealed class BookItemDto : LibraryResourceDto<BookItemEntity>
{
    public Guid BookId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public Guid? ExpectedShelfLevelId { get; set; }
    public Guid? CurrentShelfLevelId { get; set; }
    public DateTime? LastInventoryAtUtc { get; set; }
    public string Status { get; set; } = "Available";
    public override BookItemEntity ToEntity() => new(BookId, Barcode, ExpectedShelfLevelId, CurrentShelfLevelId, Status);
}

public sealed class EndpointDto : LibraryResourceDto<EndpointEntity>
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EndpointType { get; set; } = "WAYPOINT";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Theta { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public override EndpointEntity ToEntity() => new(MapId, Name, PositionX, PositionY, Theta, Description, EndpointType);
}

public sealed class EndpointGroupDto : LibraryResourceDto<EndpointGroupEntity>
{
    public string Name { get; set; } = string.Empty;
    public override EndpointGroupEntity ToEntity() => new(Name);
}

public sealed class EndpointGroupMemberDto : LibraryResourceDto<EndpointGroupMemberEntity>
{
    public Guid EndpointGroupId { get; set; }
    public Guid EndpointId { get; set; }
    public override EndpointGroupMemberEntity ToEntity() => new(EndpointGroupId, EndpointId);
}

public sealed class FloorDto : LibraryResourceDto<FloorEntity>
{
    public string Name { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public string? Description { get; set; }
    public override FloorEntity ToEntity() => new(Name, FloorNumber, Description);
}

public sealed class ShelfDto : LibraryResourceDto<ShelfEntity>
{
    public Guid AisleId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public override ShelfEntity ToEntity() => new(AisleId, Code, Name, EndpointId);
}

public sealed class ShelfLevelDto : LibraryResourceDto<ShelfLevelEntity>
{
    public Guid ShelfId { get; set; }
    public int LevelNumber { get; set; }
    public override ShelfLevelEntity ToEntity() => new(ShelfId, LevelNumber);
}

public sealed class ZoneDto : LibraryResourceDto<ZoneEntity>
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ZoneType { get; set; } = string.Empty;
    public double MaxSpeed { get; set; }
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public double RotationAngle { get; set; }
    public bool IsActive { get; set; } = true;
    public override ZoneEntity ToEntity() => new(MapId, Name, ZoneType, MinX, MinY, MaxX, MaxY,
        MaxSpeed, Description, Width, Height, RotationAngle);
}
