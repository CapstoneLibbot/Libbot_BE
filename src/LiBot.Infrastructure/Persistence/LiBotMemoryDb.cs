namespace LiBot.Infrastructure.Persistence;

public class LiBotMemoryDb
{
    private readonly Dictionary<string, object> _store = new();

    public void Set<T>(string key, T value)
    {
        _store[key] = value!;
    }

    public T? Get<T>(string key)
    {
        return _store.TryGetValue(key, out var value) ? (T?)value : default;
    }
}
