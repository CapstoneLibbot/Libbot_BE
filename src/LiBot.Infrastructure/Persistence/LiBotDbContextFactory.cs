using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text;

namespace LiBot.Infrastructure.Persistence;

/// <summary>Creates the context for EF tooling without starting API hosted services.</summary>
public sealed class LiBotDbContextFactory : IDesignTimeDbContextFactory<LiBotDbContext>
{
    public LiBotDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var options = new DbContextOptionsBuilder<LiBotDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new LiBotDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LiBot")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:LiBot");
        if (!string.IsNullOrWhiteSpace(connectionString)) return connectionString;

        foreach (var startPath in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                var path = Path.Combine(directory.FullName, ".env");
                if (File.Exists(path))
                {
                    var line = File.ReadLines(path, Encoding.UTF8)
                        .FirstOrDefault(value => value.TrimStart().StartsWith("ConnectionStrings__LiBot=", StringComparison.Ordinal));
                    if (line is not null)
                    {
                        connectionString = line[(line.IndexOf('=') + 1)..].Trim();
                        if (connectionString.Length >= 2
                            && connectionString[0] == connectionString[^1]
                            && connectionString[0] is '\'' or '"')
                            connectionString = connectionString[1..^1];
                        if (!string.IsNullOrWhiteSpace(connectionString)) return connectionString;
                    }
                }

                directory = directory.Parent;
            }
        }

        if (OperatingSystem.IsWindows())
            return "Server=(localdb)\\mssqllocaldb;Database=LiBotLogicalErd;Trusted_Connection=True;TrustServerCertificate=True";

        throw new InvalidOperationException(
            "Set ConnectionStrings__LiBot in the environment or the repository .env file before running EF migrations.");
    }
}
