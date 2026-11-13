using LiBot.Domain.Common;

namespace LiBot.Domain.Identity;

public class Notification : Entity
{
    public Guid? AccountId { get; private set; }
    public string Type { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string Severity { get; private set; }
    public bool IsRead { get; private set; }
    [System.ComponentModel.DataAnnotations.Schema.Column("ReadAt")]
    public DateTime? ReadAtUtc { get; private set; }

    public Notification(string type, string title, string message, string severity, Guid? accountId = null)
    {
        Type = Required(type, nameof(type));
        Title = Required(title, nameof(title));
        Message = Required(message, nameof(message));
        Severity = Required(severity, nameof(severity));
        AccountId = accountId;
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }

    private static string Required(string value, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.", parameterName) : value.Trim();
}
