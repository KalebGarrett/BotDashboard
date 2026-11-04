using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BotDashboard.App.Data;

/// <summary>
/// Lets <c>dotnet ef</c> build a <see cref="BotHarborDbContext"/> without starting the web host.
/// The connection string here is never used to talk to a real server during scaffolding, it only
/// has to be well formed so Npgsql can resolve the provider's model conventions.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BotHarborDbContext>
{
    public BotHarborDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=botharbor;Username=botharbor;Password=design-time";

        var options = new DbContextOptionsBuilder<BotHarborDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BotHarborDbContext(options);
    }
}
