namespace LiBot.Infrastructure.Repositories;

public class UserRepository
{
    private readonly List<string> _users = [];

    public void Add(string user)
    {
        _users.Add(user);
    }

    public IReadOnlyCollection<string> GetAll()
    {
        return _users.AsReadOnly();
    }
}
