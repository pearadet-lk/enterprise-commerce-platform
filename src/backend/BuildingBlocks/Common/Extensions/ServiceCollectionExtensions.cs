using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Constants;
using System.Security.Claims;

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
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = audience,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                if (!string.IsNullOrWhiteSpace(validIssuer))
                {
                    options.TokenValidationParameters.ValidIssuer = validIssuer;
                }
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireAssertion(context =>
                context.User.IsInRole(Roles.Admin) ||
                context.User.HasClaim("role", Roles.Admin) ||
                context.User.HasClaim(ClaimTypes.Role, Roles.Admin)));

            options.AddPolicy("ManagerOrAdmin", policy => policy.RequireAssertion(context =>
                context.User.IsInRole(Roles.Admin) ||
                context.User.IsInRole(Roles.Manager) ||
                context.User.HasClaim("role", Roles.Admin) ||
                context.User.HasClaim("role", Roles.Manager) ||
                context.User.HasClaim(ClaimTypes.Role, Roles.Admin) ||
                context.User.HasClaim(ClaimTypes.Role, Roles.Manager)));
        });

        return services;
    }
}
