using Common.Extensions;

var builder = WebApplication.CreateBuilder(args);

var authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5001";
var validIssuer = builder.Configuration["Identity:ValidIssuer"];

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddGatewayAuthentication(authority, validIssuer);

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
        policy.WithOrigins(
                "http://localhost:9200",
                "https://localhost:9200",
                "http://127.0.0.1:9200",
                "https://127.0.0.1:9200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "gateway" }));

app.Run();
