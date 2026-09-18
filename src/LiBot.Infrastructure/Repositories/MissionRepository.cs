namespace LiBot.Infrastructure.Repositories;

public class MissionRepository
{
    private readonly Dictionary<Guid, string> _missions = new();

    public void Add(Guid id, string name)
    {
        _missions[id] = name;
    }

    public string? GetById(Guid id)
    {
        return _missions.TryGetValue(id, out var name) ? name : null;
    }
}
