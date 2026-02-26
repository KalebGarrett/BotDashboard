using BotDashboard.App.Components;
using BotDashboard.App.Components.Account;
using BotDashboard.App.Credentials;
using BotDashboard.App.Data;
using BotDashboard.App.Services;
using DigitalOcean.API;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddDbContext<BotHarborDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAccountServices();

app.Run();