using System.Reflection;
using LiBot.Domain.Common;
using AisleEntity = LiBot.Domain.Library.Aisle;

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

    public void ApplyTo(TEntity entity)
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
    public Guid? EndpointId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public override AisleEntity ToEntity() => new(FloorId, Code, Name, EndpointId, Description);
}
