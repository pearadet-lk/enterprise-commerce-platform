using Common.Extensions;
using Microsoft.EntityFrameworkCore;
using Orders.API.Infrastructure;
using SharedKernel.Constants;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var authority = builder.Configuration["Identity:Authority"] ?? "http://localhost:5001";
var validIssuer = builder.Configuration["Identity:ValidIssuer"];

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddHttpClient("catalog");
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddApiAuthentication(authority, ApiScopes.OrdersApi, validIssuer);

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.MapOpenApi();
}

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await OrdersSeed.InitializeAsync(app.Services);

app.Run();
