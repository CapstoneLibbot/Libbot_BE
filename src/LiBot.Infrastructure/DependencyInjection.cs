using LiBot.Infrastructure.Persistence;
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
        return services;
    }
}