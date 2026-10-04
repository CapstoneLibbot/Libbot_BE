using LiBot.Domain.Common;

namespace LiBot.Domain.Identity;

public class AuditLog : Entity
{
    public Guid? AccountId { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? OldValue { get; private set; }
    public string? NewValue { get; private set; }

    public AuditLog(string action, string entityType, Guid? accountId = null, Guid? entityId = null,
        string? oldValue = null, string? newValue = null)
    {
        Action = Required(action, nameof(action));
        EntityType = Required(entityType, nameof(entityType));
        AccountId = accountId;
        EntityId = entityId;
        OldValue = oldValue;
        NewValue = newValue;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}