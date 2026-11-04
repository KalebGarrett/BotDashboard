using BotDashboard.App.Components;
using BotDashboard.App.Components.Account;
using BotDashboard.App.Credentials;
using BotDashboard.App.Data;
using BotDashboard.App.Services;
using DigitalOcean.API;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Bot Harbor is self-hosted behind a Tailscale sidecar on the Cortex NAS. `tailscale serve`
// terminates TLS and forwards plain HTTP to Kestrel, so the app must trust the forwarded headers
// and must not try to issue its own HTTPS redirects.
var behindReverseProxy = builder.Configuration.GetValue("BotHarbor:BehindReverseProxy", false);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

if (behindReverseProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // The only upstream is the Tailscale sidecar sharing this pod's loopback, and the Docker
        // bridge gateway. Clearing the defaults lets those private addresses be trusted.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Identity's auth cookie is encrypted with Data Protection keys. Left on the default in-container
// path those keys die with the container, logging everyone out on every redeploy, so they are
// persisted onto the NAS volume instead.
var dataProtectionKeyPath = builder.Configuration["BotHarbor:DataProtectionKeyPath"];

if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    var keyDirectory = Directory.CreateDirectory(dataProtectionKeyPath);

    builder.Services.AddDataProtection()
        .SetApplicationName("BotHarbor")
        .PersistKeysToFileSystem(keyDirectory);
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "No database connection string. Set ConnectionStrings__DefaultConnection to the PostgreSQL " +
        "instance (see .env.example).");
}

builder.Services.AddDbContext<BotHarborDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<BotHarborUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<BotHarborDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton(_ =>
{
    var apiToken = builder.Configuration["ApiTokens:DigitalOcean"];

    return new DigitalOceanCredentials(apiToken);
});

builder.Services.AddSingleton(_ =>
{
    var token = builder.Configuration["ApiTokens:DigitalOcean"];
    return new DigitalOceanClient(token);
});

builder.Services.AddSingleton(_ =>
{
    var id = builder.Configuration["SSH:Id"];
    var host = builder.Configuration["SSH:Host"];
    var username = builder.Configuration["SSH:Username"];
    var password = builder.Configuration["SSH:Password"];

    return new DigitalOceanDropletCredentials(id, host, username, password);
});

builder.Services.AddScoped<DigitalOceanService>();

builder.Services.AddScoped<DockerService>();
builder.Services.AddScoped<UbuntuService>();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
    config.SnackbarConfiguration.ShowCloseIcon = true;
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BotHarborDbContext>("database");

var app = builder.Build();

if (behindReverseProxy)
{
    app.UseForwardedHeaders();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

if (!behindReverseProxy)
{
    app.UseHttpsRedirection();

    if (!app.Environment.IsDevelopment())
    {
        // The default HSTS value is 30 days. You may want to change this for production scenarios,
        // see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapHealthChecks("/healthz").AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAccountServices();

var startupLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("BotHarbor.Startup");

// The dashboard's whole job is driving the droplet, but Switch.OnInitialized starts false so
// nothing is fetched until the operator asks. A missing token is therefore not fatal, and must
// not throw -- that would break `dotnet run` for local UI work, the same way the compose
// ${VAR:?} guards used to break `docker compose up -d postgres`. Name what is missing instead,
// once, at startup, so a half-configured deployment is obvious in `docker compose logs`.
var missingDropletSettings = new[]
    {
        ("ApiTokens:DigitalOcean", "DIGITALOCEAN_TOKEN"),
        ("SSH:Id", "DROPLET_ID"),
        ("SSH:Host", "DROPLET_HOST"),
        ("SSH:Username", "DROPLET_USERNAME"),
        ("SSH:Password", "DROPLET_PASSWORD")
    }
    .Where(setting => string.IsNullOrWhiteSpace(app.Configuration[setting.Item1]))
    .Select(setting => setting.Item2)
    .ToArray();

if (missingDropletSettings.Length > 0)
{
    startupLogger.LogWarning(
        "Running without these droplet settings: {Missing}. Sign-in and the UI work, but "
        + "listing containers or stats will fail until they are set in .env. See deploy/README.md.",
        string.Join(", ", missingDropletSettings));
}

await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, startupLogger);

app.Run();
