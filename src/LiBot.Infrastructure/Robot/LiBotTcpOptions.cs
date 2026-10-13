namespace LiBot.Infrastructure.Robot;

public sealed class LiBotTcpOptions
{
    public const string SectionName = "LiBot";
    public const int MaximumTimeoutSeconds = 86_400;

    public string Host { get; set; } = "192.168.1.8";

    public int Port { get; set; } = 9000;

    public int ConnectTimeoutSeconds { get; set; } = 5;

    public int ResponseTimeoutSeconds { get; set; } = 120;
}
