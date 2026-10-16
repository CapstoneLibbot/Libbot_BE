using LiBot.Application.Robot;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Application.Workflow.FacilityGuidance;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using LiBot.Infrastructure.Workflow;
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
                    && options.ResponseTimeoutSeconds is >= 1 and <= LiBotTcpOptions.MaximumTimeoutSeconds
                    && options.RuntimeEndpoints.All(item =>
                        !string.IsNullOrWhiteSpace(item.Key)
                        && !string.IsNullOrWhiteSpace(item.Value.Host)
                        && Uri.CheckHostName(item.Value.Host) != UriHostNameType.Unknown
                        && item.Value.Port is >= 1 and <= 65535),
                "LiBot host, port, and timeout settings must be valid.")
            .ValidateOnStart();
        services.AddSingleton<ILiBotTcpClient, LiBotTcpClient>();
        services.AddSingleton<BookGuidanceRuntimeRegistry>();
        services.AddScoped<IBookGuidanceService, BookGuidanceService>();
        services.AddScoped<IFacilityGuidanceService, FacilityGuidanceService>();

        return services;
    }
}
