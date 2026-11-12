using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LiBot.Infrastructure.Persistence;

/// <summary>Creates the context for EF tooling without starting API hosted services.</summary>
public sealed class LiBotDbContextFactory : IDesignTimeDbContextFactory<LiBotDbContext>
{
    public LiBotDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LiBotDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=LiBotLogicalErd;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new LiBotDbContext(options);
    }
}
