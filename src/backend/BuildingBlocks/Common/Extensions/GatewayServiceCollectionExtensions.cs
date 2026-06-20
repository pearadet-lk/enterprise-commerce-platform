using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Common.Extensions;

public static class GatewayServiceCollectionExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services,
        string authority,
        string? validIssuer = null)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = authority;
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };

                if (!string.IsNullOrWhiteSpace(validIssuer))
                {
                    options.TokenValidationParameters.ValidIssuer = validIssuer;
                }
            });

        services.AddAuthorization();
        return services;
    }
}
