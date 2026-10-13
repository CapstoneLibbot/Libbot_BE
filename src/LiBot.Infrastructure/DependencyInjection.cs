using LiBot.Application.Robot;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LiBot")
            ?? throw new InvalidOperationException("Connection string 'LiBot' was not configured.");

        services.AddDbContext<LiBotDbContext>(options => options.UseSqlServer(connectionString));
        services.AddOptions<LiBotTcpOptions>()
            .Bind(configuration.GetSection(LiBotTcpOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host)
                    && Uri.CheckHostName(options.Host) != UriHostNameType.Unknown
                    && options.Port is >= 1 and <= 65535
                    && options.ConnectTimeoutSeconds is >= 1 and <= LiBotTcpOptions.MaximumTimeoutSeconds
                    && options.ResponseTimeoutSeconds is >= 1 and <= LiBotTcpOptions.MaximumTimeoutSeconds,
                "LiBot host, port, and timeout settings must be valid.")
            .ValidateOnStart();
        services.AddSingleton<ILiBotTcpClient, LiBotTcpClient>();

        return services;
    }
}