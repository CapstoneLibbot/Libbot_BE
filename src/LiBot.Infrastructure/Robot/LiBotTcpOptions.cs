namespace LiBot.Infrastructure.Robot;

public sealed class LiBotTcpOptions
{
    public const string SectionName = "LiBot";
    public const int MaximumTimeoutSeconds = 86_400;

    public string Host { get; set; } = "192.168.1.8";

    public int Port { get; set; } = 9000;

    public int ConnectTimeoutSeconds { get; set; } = 5;

    public int ResponseTimeoutSeconds { get; set; } = 120;

    public Dictionary<string, LiBotTcpRuntimeEndpoint> RuntimeEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LiBotTcpRuntimeEndpoint
{
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 9000;
}
