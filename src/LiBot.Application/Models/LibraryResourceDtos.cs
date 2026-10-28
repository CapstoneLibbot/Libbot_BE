using System.Reflection;
using LiBot.Domain.Common;
using LiBot.Domain.Library;

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

public sealed class AisleDto : LibraryResourceDto<Aisle>
{
    public Guid FloorId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override Aisle ToEntity() => new(FloorId, Code, Name, EndpointId, Description);
}

public sealed class AreaDto : LibraryResourceDto<Area>
{
    public Guid FloorId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override Area ToEntity() => new(FloorId, Name, Description, EndpointId);
}

public sealed class BookDto : LibraryResourceDto<Book>
{
    public Guid? CategoryId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Image { get; set; }
    public override Book ToEntity() => new(Isbn, Title, Author, CategoryId, Publisher, PublicationYear, Image);
}

public sealed class BookCategoryDto : LibraryResourceDto<BookCategory>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override BookCategory ToEntity() => new(Name, Description);
}

public sealed class BookItemDto : LibraryResourceDto<BookItem>
{
    public Guid BookId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public Guid? ExpectedShelfLevelId { get; set; }
    public Guid? CurrentShelfLevelId { get; set; }
    public DateTime? LastInventoryAtUtc { get; set; }
    public string Status { get; set; } = "Available";
    public override BookItem ToEntity() => new(BookId, Barcode, ExpectedShelfLevelId, CurrentShelfLevelId, Status);
}

public sealed class EndpointDto : LibraryResourceDto<Endpoint>
{
    public Guid MapId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EndpointType { get; set; } = "WAYPOINT";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Theta { get; set; }
    public bool IsActive { get; set; } = true;
    public override Endpoint ToEntity() => new(MapId, Name, PositionX, PositionY, Theta, EndpointType);

    public override void ApplyTo(Endpoint entity)
    {
        var validated = ToEntity();
        Name = validated.Name;
        EndpointType = validated.EndpointType;
        base.ApplyTo(entity);
    }
}

public sealed class EndpointGroupDto : LibraryResourceDto<EndpointGroup>
{
    public string Name { get; set; } = string.Empty;
    public override EndpointGroup ToEntity() => new(Name);
}

public sealed class EndpointGroupMemberDto : LibraryResourceDto<EndpointGroupMember>
{
    public Guid EndpointGroupId { get; set; }
    public Guid EndpointId { get; set; }
    public override EndpointGroupMember ToEntity() => new(EndpointGroupId, EndpointId);
}

public sealed class FloorDto : LibraryResourceDto<Floor>
{
    public string Name { get; set; } = string.Empty;
    public int FloorNumber { get; set; }
    public override Floor ToEntity() => new(Name, FloorNumber);
}

public sealed class ShelfDto : LibraryResourceDto<Shelf>
{
    public Guid AisleId { get; set; }
    public Guid? EndpointId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public override Shelf ToEntity() => new(AisleId, Code, Name, EndpointId);
}

public sealed class ShelfLevelDto : LibraryResourceDto<ShelfLevel>
{
    public Guid ShelfId { get; set; }
    public int LevelNumber { get; set; }
    public override ShelfLevel ToEntity() => new(ShelfId, LevelNumber);
}

public sealed class ZoneDto : LibraryResourceDto<Zone>
{
    public Guid MapId { get; set; }
    public string ZoneType { get; set; } = string.Empty;
    public double MaxSpeed { get; set; }
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public bool IsActive { get; set; } = true;
    public override Zone ToEntity() => new(MapId, ZoneType, MinX, MinY, MaxX, MaxY, MaxSpeed);

    public override void ApplyTo(Zone entity)
    {
        var validated = ToEntity();
        ZoneType = validated.ZoneType;
        MaxSpeed = validated.MaxSpeed;
        base.ApplyTo(entity);
    }
}
