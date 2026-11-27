namespace LiBot.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    [System.ComponentModel.DataAnnotations.Schema.Column("CreatedAt")]
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    [System.ComponentModel.DataAnnotations.Schema.Column("UpdatedAt")]
    public DateTime? UpdatedAtUtc { get; protected set; }

    public void SetUpdated()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
