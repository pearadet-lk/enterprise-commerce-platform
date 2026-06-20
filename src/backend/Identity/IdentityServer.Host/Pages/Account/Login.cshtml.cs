using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using IdentityServer.Host.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Host.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IIdentityServerInteractionService interaction) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberLogin { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByNameAsync(Input.Username)
            ?? await userManager.FindByEmailAsync(Input.Username);

        if (user is null || !user.IsActive)
        {
            ErrorMessage = "Invalid credentials.";
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            user.UserName!,
            Input.Password,
            Input.RememberLogin,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var context = await interaction.GetAuthorizationContextAsync(ReturnUrl, CancellationToken.None);
            if (context is not null)
            {
                return Redirect(ReturnUrl);
            }

            if (Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            return Redirect("~/");
        }

        ErrorMessage = result.IsLockedOut ? "Account locked out." : "Invalid credentials.";
        return Page();
    }
}
