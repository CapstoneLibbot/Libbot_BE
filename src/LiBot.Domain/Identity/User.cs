namespace LiBot.Domain.Identity;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? Username { get; private set; }
    public string? PasswordHash { get; private set; }
    public string? Role { get; private set; }
    public string Email { get; private set; }
    public string FullName { get; private set; }
    public string? Phone { get; private set; }
    public UserStatus Status { get; private set; }
    public bool IsActive => Status == UserStatus.Active;
    [System.ComponentModel.DataAnnotations.Schema.Column("CreatedAt")]
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    [System.ComponentModel.DataAnnotations.Schema.Column("UpdatedAt")]
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    [System.ComponentModel.DataAnnotations.Schema.Column("LastLoginAt")]
    public DateTime? LastLoginAtUtc { get; private set; }

    public User(string email, string fullName, string? username = null, string? passwordHash = null,
        string? role = null, string? phone = null, UserStatus status = UserStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        Email = email.Trim();
        FullName = fullName.Trim();
        Username = username?.Trim();
        PasswordHash = passwordHash;
        Role = role?.Trim();
        Phone = phone?.Trim();
        Status = status;
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Lock()
    {
        Status = UserStatus.Locked;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
