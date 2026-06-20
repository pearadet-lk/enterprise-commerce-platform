using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityServer.Host.Data;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace IdentityServer.Host.Services;

public class ProfileService(
    UserManager<ApplicationUser> userManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory) : IProfileService
{
    public async Task GetProfileDataAsync(ProfileDataRequestContext context, CancellationToken cancellationToken = default)
    {
        var sub = context.Subject?.FindFirst("sub")?.Value;
        if (sub is null)
        {
            return;
        }

        var user = await userManager.FindByIdAsync(sub);
        if (user is null)
        {
            return;
        }

        var principal = await claimsFactory.CreateAsync(user);
        var claims = principal.Claims.ToList();

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim("name", user.FullName));
        }

        context.IssuedClaims.AddRange(claims);
    }

    public async Task IsActiveAsync(IsActiveContext context, CancellationToken cancellationToken = default)
    {
        var sub = context.Subject?.FindFirst("sub")?.Value;
        if (sub is null)
        {
            context.IsActive = false;
            return;
        }

        var user = await userManager.FindByIdAsync(sub);
        context.IsActive = user is { IsActive: true };
    }
}
