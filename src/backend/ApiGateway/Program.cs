using Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddPlatformOpenTelemetry("api-gateway");
builder.AddPlatformCrossCutting();

var authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5001";
var validIssuer = builder.Configuration["Identity:ValidIssuer"];

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddGatewayAuthentication(authority, validIssuer);

builder.Services.AddPlatformCors(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

app.UsePlatformMiddleware();
app.UsePlatformCors();

app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

app.Run();
