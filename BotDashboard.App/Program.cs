using BotDashboard.App;
using BotDashboard.App.Components;
using BotDashboard.App.Components.Account;
using BotDashboard.App.Credentials;
using BotDashboard.App.Data;
using BotDashboard.App.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Bot Harbor is self-hosted on the Cortex NAS behind a Cloudflare Tunnel. `cloudflared`
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

// "Remember me" asks Identity for a persistent cookie instead of a session one; this is how
// long that cookie is then good for. Left implicit it was the framework's 14 days, which is
// short for a dashboard exactly one person ever signs in to, and being implicit made the
// setting impossible to reason about from the code. Sliding, so active use keeps it alive.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
});

if (behindReverseProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // The only upstream is the cloudflared sidecar sharing this container's loopback, and
        // the Docker bridge gateway. Clearing the defaults lets those private addresses be
        // trusted.
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

// Cortex, the NAS. Everything container-related and the whole Stats page run here.
builder.Services.AddSingleton(_ =>
{
    var host = builder.Configuration["Nas:Host"];
    var username = builder.Configuration["Nas:Username"];
    var password = builder.Configuration["Nas:Password"];
    var diskPath = builder.Configuration["Nas:DiskPath"];

    return new NasCredentials(host, username, password, diskPath);
});

// The home server PC, a Windows box running Lavalink and Ollama. Only the Ollama controls use it.
builder.Services.AddSingleton(_ =>
{
    var host = builder.Configuration["ServerPc:Host"];
    var username = builder.Configuration["ServerPc:Username"];
    var password = builder.Configuration["ServerPc:Password"];

    return new ServerPcCredentials(host, username, password);
});

builder.Services.AddSingleton(_ =>
{
    var host = builder.Configuration["Ollama:Host"];
    var port = builder.Configuration.GetValue("Ollama:Port", 11434);
    var probeContainer = builder.Configuration["Ollama:ProbeContainer"];

    return new OllamaEndpoint(host, port, probeContainer);
});

builder.Services.AddScoped<DockerService>();
builder.Services.AddScoped<UbuntuService>();
builder.Services.AddScoped<OllamaService>();
builder.Services.AddSingleton<PreferencesService>();

builder.Services.AddSingleton<NotificationService>();

// Singleton on purpose: every circuit shares one SSH connection to the server PC, rather than
// each Ollama button paying for its own Windows login.
builder.Services.AddSingleton<ServerPcSshSession>();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.MaxDisplayedSnackbars = 5;
    config.SnackbarConfiguration.ShowCloseIcon = true;
});

// Swap MudBlazor's own ISnackbar registration for a decorator, so every page's existing
// Snackbar.Add also files a notification-bell row. Done here rather than by editing a dozen
// call sites, which would leave the next page added to the app silently out of step.
// The original descriptor is reused to build the inner service: MudBlazor's SnackbarService
// constructor has changed shape between versions, and reconstructing it by hand would be a
// package upgrade away from breaking.
var snackbarDescriptor = builder.Services.Last(service => service.ServiceType == typeof(ISnackbar));
builder.Services.Remove(snackbarDescriptor);
builder.Services.Add(new ServiceDescriptor(typeof(ISnackbar), provider =>
{
    var inner = snackbarDescriptor.ImplementationFactory is not null
        ? (ISnackbar)snackbarDescriptor.ImplementationFactory(provider)
        : snackbarDescriptor.ImplementationInstance is ISnackbar instance
            ? instance
            : (ISnackbar)ActivatorUtilities.CreateInstance(provider, snackbarDescriptor.ImplementationType!);

    return new BellSyncedSnackbar(inner, provider.GetRequiredService<NotificationService>());
}, snackbarDescriptor.Lifetime));

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

// Seed the auto-load switch from disk so the operator's choice survives a restart.
var preferences = app.Services.GetRequiredService<PreferencesService>();
preferences.Load();
Switch.OnInitialized = preferences.AutoLoadData;

// Notifications filed before the last restart are still worth reading: the point of the bell
// is to survive the browser being closed, which includes a redeploy.
app.Services.GetRequiredService<NotificationService>().Load();

var startupLogger = app.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("BotHarbor.Startup");

// The dashboard's whole job is driving Cortex, but Switch.OnInitialized starts false so
// nothing is fetched until the operator asks. A missing token is therefore not fatal, and must
// not throw -- that would break `dotnet run` for local UI work, the same way the compose
// ${VAR:?} guards used to break `docker compose up -d postgres`. Name what is missing instead,
// once, at startup, so a half-configured deployment is obvious in `docker compose logs`.
// Cortex first: without it the container grid and the whole Stats page are blind, which is a
// far bigger hole than the Ollama controls below.
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
        + "to the server PC. Set OLLAMA_HOST in .env to the server PC's tailnet address.");
}

var missingServerPcSettings = new[]
    {
        ("ServerPc:Host", "SERVER_PC_HOST"),
        ("ServerPc:Username", "SERVER_PC_USERNAME"),
        ("ServerPc:Password", "SERVER_PC_PASSWORD")
    }
    .Where(setting => string.IsNullOrWhiteSpace(app.Configuration[setting.Item1]))
    .Select(setting => setting.Item2)
    .ToArray();

if (missingServerPcSettings.Length > 0)
{
    startupLogger.LogWarning(
        "Running without these server PC settings: {Missing}. The Ollama controls need them; "
        + "everything else reads from Cortex. See deploy/README.md.",
        string.Join(", ", missingServerPcSettings));
}

await DatabaseInitializer.InitializeAsync(app.Services, app.Configuration, startupLogger);

app.Run();
