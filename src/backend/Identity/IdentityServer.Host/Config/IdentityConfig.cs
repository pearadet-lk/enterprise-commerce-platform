using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Configuration;
using ApiScopeNames = SharedKernel.Constants.ApiScopes;

namespace IdentityServer.Host.Config;

public static class IdentityConfig
{
    private static readonly string[] DefaultRedirectUris =
    [
        "http://localhost:9200/auth-callback",
        "https://localhost:9200/auth-callback",
        "http://127.0.0.1:9200/auth-callback",
        "https://127.0.0.1:9200/auth-callback"
    ];

    private static readonly string[] DefaultPostLogoutUris =
    [
        "http://localhost:9200",
        "https://localhost:9200",
        "http://127.0.0.1:9200",
        "https://127.0.0.1:9200"
    ];

    private static readonly string[] DefaultCorsOrigins =
    [
        "http://localhost:9200",
        "https://localhost:9200",
        "http://127.0.0.1:9200",
        "https://127.0.0.1:9200"
    ];

    public static IEnumerable<IdentityResource> IdentityResources =>
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
        new IdentityResource(
            name: "roles",
            displayName: "Roles",
            userClaims: [JwtClaimTypes.Role])
    ];

    public static IEnumerable<ApiScope> ApiScopes =>
    [
        new ApiScope(ApiScopeNames.CatalogApi, "Catalog API"),
        new ApiScope(ApiScopeNames.OrdersApi, "Orders API"),
        new ApiScope(ApiScopeNames.UsersApi, "Users API")
    ];

    public static IEnumerable<ApiResource> ApiResources =>
    [
        new ApiResource(ApiScopeNames.CatalogApi, "Catalog API")
        {
            Scopes = { ApiScopeNames.CatalogApi },
            UserClaims = { JwtClaimTypes.Role, JwtClaimTypes.Name }
        },
        new ApiResource(ApiScopeNames.OrdersApi, "Orders API")
        {
            Scopes = { ApiScopeNames.OrdersApi },
            UserClaims = { JwtClaimTypes.Role, JwtClaimTypes.Name }
        },
        new ApiResource(ApiScopeNames.UsersApi, "Users API")
        {
            Scopes = { ApiScopeNames.UsersApi },
            UserClaims = { JwtClaimTypes.Role, JwtClaimTypes.Name }
        }
    ];

    public static IEnumerable<Client> GetClients(IConfiguration configuration)
    {
        var redirectUris = ReadUris(configuration, "SpaClient:RedirectUris", DefaultRedirectUris);
        var postLogoutUris = ReadUris(configuration, "SpaClient:PostLogoutRedirectUris", DefaultPostLogoutUris);
        var corsOrigins = ReadUris(configuration, "SpaClient:AllowedCorsOrigins", DefaultCorsOrigins);

        return
        [
            new Client
            {
                ClientId = "angular-spa",
                ClientName = "Enterprise Commerce Angular SPA",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                AllowOfflineAccess = true,
                RedirectUris = redirectUris,
                PostLogoutRedirectUris = postLogoutUris,
                AllowedCorsOrigins = corsOrigins,
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "roles",
                    ApiScopeNames.CatalogApi,
                    ApiScopeNames.OrdersApi,
                    ApiScopeNames.UsersApi
                },
                AccessTokenLifetime = 3600,
                RefreshTokenUsage = TokenUsage.OneTimeOnly,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 1296000
            }
        ];
    }

    private static ICollection<string> ReadUris(
        IConfiguration configuration,
        string sectionName,
        IEnumerable<string> defaults)
    {
        var configured = configuration.GetSection(sectionName).Get<string[]>();
        if (configured is null || configured.Length == 0)
        {
            return defaults.ToList();
        }

        return configured.Concat(defaults).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
