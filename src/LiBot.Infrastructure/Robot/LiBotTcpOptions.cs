namespace LiBot.Infrastructure.Robot;

public sealed class LiBotTcpOptions
{
    public const string SectionName = "LiBot";
    public const int MaximumTimeoutSeconds = 86_400;

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }

    public int ConnectTimeoutSeconds { get; set; }

    public int ResponseTimeoutSeconds { get; set; }

    public Dictionary<string, LiBotTcpRuntimeEndpoint> RuntimeEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class LiBotTcpRuntimeEndpoint
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; }
}
