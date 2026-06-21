using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Extensions;

public static class CorsExtensions
{
    private static readonly string[] DefaultOrigins =
    [
        "http://localhost:9200",
        "https://localhost:9200",
        "http://127.0.0.1:9200",
        "https://127.0.0.1:9200"
    ];

    public static IServiceCollection AddPlatformCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is null || origins.Length == 0)
        {
            var csv = configuration["Cors:AllowedOriginsCsv"];
            if (!string.IsNullOrWhiteSpace(csv))
            {
                origins = csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }
        }

        origins ??= DefaultOrigins;

        services.AddCors(options =>
        {
            options.AddPolicy("spa", policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                if (configuration.GetValue("Cors:AllowCredentials", false))
                {
                    policy.AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UsePlatformCors(this IApplicationBuilder app) =>
        app.UseCors("spa");
}
