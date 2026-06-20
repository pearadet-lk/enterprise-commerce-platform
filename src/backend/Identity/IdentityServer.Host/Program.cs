using Duende.IdentityServer;
using IdentityServer.Host;
using IdentityServer.Host.Config;
using IdentityServer.Host.Data;
using IdentityServer.Host.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "dpkeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("IdentityServer");

builder.Services
    .AddIdentityServer(options =>
    {
        options.IssuerUri = builder.Configuration["Identity:IssuerUri"] ?? "http://localhost:5001";
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
        options.EmitStaticAudienceClaim = false;
        options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
        options.Authentication.CheckSessionCookieSameSiteMode = SameSiteMode.Lax;
    })
    .AddInMemoryIdentityResources(IdentityConfig.IdentityResources)
    .AddInMemoryApiScopes(IdentityConfig.ApiScopes)
    .AddInMemoryApiResources(IdentityConfig.ApiResources)
    .AddInMemoryClients(IdentityConfig.Clients)
    .AddAspNetIdentity<ApplicationUser>()
    .AddProfileService<ProfileService>();

// Duende re-applies SameSite=None for the ASP.NET Identity cookie; override after registration.
builder.Services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy.WithOrigins(
                "http://localhost:9200",
                "https://localhost:9200",
                "http://127.0.0.1:9200",
                "https://127.0.0.1:9200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRazorPages();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseDeveloperExceptionPage();
}

app.UseForwardedHeaders();
app.UseCors("spa");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseIdentityServer();
app.UseAuthorization();
app.MapRazorPages();

await SeedData.InitializeAsync(app.Services);

app.Run();
