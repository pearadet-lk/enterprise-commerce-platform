using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using ApiScopeNames = SharedKernel.Constants.ApiScopes;

namespace IdentityServer.Host.Config;

public static class IdentityConfig
{
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

    public static IEnumerable<Client> Clients =>
    [
        new Client
        {
            ClientId = "angular-spa",
            ClientName = "Enterprise Commerce Angular SPA",
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireClientSecret = false,
            AllowOfflineAccess = true,
            RedirectUris =
            {
                "http://localhost:9200/auth-callback",
                "https://localhost:9200/auth-callback",
                "http://127.0.0.1:9200/auth-callback",
                "https://127.0.0.1:9200/auth-callback"
            },
            PostLogoutRedirectUris =
            {
                "http://localhost:9200",
                "https://localhost:9200",
                "http://127.0.0.1:9200",
                "https://127.0.0.1:9200"
            },
            AllowedCorsOrigins =
            {
                "http://localhost:9200",
                "https://localhost:9200",
                "http://127.0.0.1:9200",
                "https://127.0.0.1:9200"
            },
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
