using IdentityServer.Host.Data;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Constants;

namespace IdentityServer.Host;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureCreatedAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { Roles.Admin, Roles.Manager, Roles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        await EnsureUserAsync(
            userManager,
            email: "admin@enterprise.local",
            userName: "admin",
            password: "Admin123!",
            fullName: "Platform Admin",
            roles: [Roles.Admin]);

        await EnsureUserAsync(
            userManager,
            email: "manager@enterprise.local",
            userName: "manager",
            password: "Manager123!",
            fullName: "Order Manager",
            roles: [Roles.Manager]);

        await EnsureUserAsync(
            userManager,
            email: "user@enterprise.local",
            userName: "user",
            password: "User123!",
            fullName: "Standard User",
            roles: [Roles.User]);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string userName,
        string password,
        string fullName,
        IEnumerable<string> roles)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Email = email,
            UserName = userName,
            EmailConfirmed = true,
            FullName = fullName,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create seed user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await userManager.AddToRolesAsync(user, roles);
    }
}
