using Catalog.API.Infrastructure;
using Common.Extensions;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.AddPlatformOpenTelemetry("catalog-api");
builder.AddPlatformCrossCutting();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5001";
var validIssuer = builder.Configuration["Identity:ValidIssuer"];

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApiAuthentication(authority, ApiScopes.CatalogApi, validIssuer);

builder.Services.AddPlatformCors(builder.Configuration);

var app = builder.Build();

app.UsePlatformMiddleware();
app.UsePlatformCors();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await CatalogSeed.InitializeAsync(app.Services);

app.Run();
