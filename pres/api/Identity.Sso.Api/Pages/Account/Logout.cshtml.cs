using Identity.Sso.Application.Behaviors.Accounts.Commands.SignOut;
using Identity.Sso.Application.Utils.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identity.Sso.Api.Pages.Account;

[AllowAnonymous]
public class LogoutModel(IMediator mediator) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// When the sign-out was initiated by a client, the confirmation must post back to the end session
    /// endpoint so that OpenIddict can validate post_logout_redirect_uri and echo the state parameter.
    /// </summary>
    public string PostTarget { get; private set; } = "/Account/Logout";

    public IActionResult OnGet()
    {
        if (!string.IsNullOrEmpty(ReturnUrl))
        {
            if (!Url.IsLocalUrl(ReturnUrl))
                return BadRequest();

            PostTarget = ReturnUrl;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await mediator.SendAsync(new SignOutCommand(), cancellationToken);
        return RedirectToPage("/Account/Login");
    }
}
