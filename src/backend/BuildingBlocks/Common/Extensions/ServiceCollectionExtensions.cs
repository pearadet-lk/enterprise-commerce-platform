using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Constants;

namespace Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        string authority,
        string audience,
        string? validIssuer = null)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = audience,
                    RoleClaimType = "role"
                };

                if (!string.IsNullOrWhiteSpace(validIssuer))
                {
                    options.TokenValidationParameters.ValidIssuer = validIssuer;
                }
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole(Roles.Admin));
            options.AddPolicy("ManagerOrAdmin", policy =>
                policy.RequireRole(Roles.Admin, Roles.Manager));
        });

        return services;
    }
}
