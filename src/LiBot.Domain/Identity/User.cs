namespace LiBot.Domain.Identity;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Email { get; private set; }
    public string FullName { get; private set; }
    public bool IsActive { get; private set; } = true;

    public User(string email, string fullName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name is required.", nameof(fullName));

        Email = email.Trim();
        FullName = fullName.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
