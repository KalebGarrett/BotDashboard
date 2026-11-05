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

// Cortex, the NAS. Everything container-related and the whole Stats page run here; the droplet
// is left with only the Manage Droplet page and the Ollama controls.
builder.Services.AddSingleton(_ =>
{
    var host = builder.Configuration["Nas:Host"];
    var username = builder.Configuration["Nas:Username"];
    var password = builder.Configuration["Nas:Password"];

    return new NasCredentials(host, username, password);
});

builder.Services.AddSingleton(_ =>
{
    var host = builder.Configuration["Ollama:Host"];
    var port = builder.Configuration.GetValue("Ollama:Port", 11434);

    return new OllamaEndpoint(host, port);
});

// Short timeout on purpose: this backs a status panel, and a dead tailnet path should report
// itself in seconds rather than leaving the page spinning on the default 100.
builder.Services.AddHttpClient("ollama", client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

builder.Services.AddScoped<DigitalOceanService>();

builder.Services.AddScoped<DockerService>();
builder.Services.AddScoped<UbuntuService>();
builder.Services.AddScoped<OllamaService>();

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

// .NET 9 moved framework assets -- blazor.web.js among them -- out of UseStaticFiles and onto
// this endpoint-routed manifest. Without it App.razor's <script src="_framework/blazor.web.js">
// 404s, Blazor never boots, and the page renders once and is then inert: no circuit, so
// MudMenu (logout) and MudHidden (the signed-in email) silently do nothing.
app.MapStaticAssets();

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
// Cortex first: without it the container grid and the whole Stats page are blind, which is a
// far bigger hole than the droplet-only features below.
var missingNasSettings = new[]
    {
        ("Nas:Host", "NAS_HOST"),
        ("Nas:Username", "NAS_USERNAME"),
        ("Nas:Password", "NAS_PASSWORD")
    }
    .Where(setting => string.IsNullOrWhiteSpace(app.Configuration[setting.Item1]))
    .Select(setting => setting.Item2)
    .ToArray();

if (missingNasSettings.Length > 0)
{
    startupLogger.LogWarning(
        "Running without these Cortex settings: {Missing}. Containers, images and the Stats "
        + "page all read from Cortex over SSH and will fail until these are set in .env.",
        string.Join(", ", missingNasSettings));
}

if (string.IsNullOrWhiteSpace(app.Configuration["Ollama:Host"]))
{
    startupLogger.LogWarning(
        "Ollama:Host is not set, so the Ollama health panel cannot test the path from Cortex "
        + "to the droplet. Set OLLAMA_HOST in .env to the droplet's tailnet address.");
}

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
        "Running without these droplet settings: {Missing}. The Manage Droplet page and the "
        + "Ollama controls need them; everything else now reads from Cortex. See deploy/README.md.",
        string.Join(", ", missingDropletSettings));
}

await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, startupLogger);

app.Run();
