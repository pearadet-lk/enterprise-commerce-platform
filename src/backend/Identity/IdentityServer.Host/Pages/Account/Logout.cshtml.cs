using Duende.IdentityServer.Services;
using IdentityServer.Host.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Host.Pages.Account;

public class LogoutModel(
    SignInManager<ApplicationUser> signInManager,
    IIdentityServerInteractionService interaction) : PageModel
{
    [BindProperty]
    public string? LogoutId { get; set; }

    public async Task<IActionResult> OnGetAsync(string? logoutId)
    {
        LogoutId = logoutId;
        await signInManager.SignOutAsync();
        await HttpContext.SignOutAsync();

        var logout = await interaction.GetLogoutContextAsync(logoutId, CancellationToken.None);
        if (!string.IsNullOrEmpty(logout?.PostLogoutRedirectUri))
        {
            return Redirect(logout.PostLogoutRedirectUri);
        }

        return Page();
    }
}
