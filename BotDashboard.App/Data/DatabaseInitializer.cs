using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BotDashboard.App.Data;

/// <summary>
/// Brings a freshly provisioned PostgreSQL database up to a usable state on startup.
/// <para>
/// Bot Harbor has no self-service registration page, so an empty database would otherwise be
/// unreachable: there would be no account to log in with. This applies any pending migrations and
/// then seeds a single bootstrap operator from configuration.
/// </para>
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration,
        ILogger logger, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        if (configuration.GetValue("Database:MigrateOnStartup", true))
        {
            var db = scope.ServiceProvider.GetRequiredService<BotHarborDbContext>();

            logger.LogInformation("Applying pending database migrations...");
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database schema is up to date.");
        }

        await SeedBootstrapUserAsync(scope.ServiceProvider, configuration, logger);
    }

    private static async Task SeedBootstrapUserAsync(IServiceProvider services, IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration["BotHarbor:BootstrapUser:Email"];
        var password = configuration["BotHarbor:BootstrapUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "No bootstrap user configured, skipping seed. Set BotHarbor:BootstrapUser:Email and " +
                ":Password to create the first operator account.");
            return;
        }

        var userManager = services.GetRequiredService<UserManager<BotHarborUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            logger.LogInformation("Bootstrap user {Email} already exists, leaving it untouched.", email);
            return;
        }

        var user = new BotHarborUser
        {
            UserName = email,
            Email = email,
            // Identity is configured with RequireConfirmedAccount, and this deployment has no mail
            // sender, so the bootstrap account is confirmed up front or it could never sign in.
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            logger.LogWarning(
                "Created bootstrap user {Email}. Change this password from Settings -> Manage Password, " +
                "then clear BotHarbor__BootstrapUser__Password from the environment.", email);
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
        logger.LogError("Failed to create bootstrap user {Email}. {Errors}", email, errors);
    }
}
